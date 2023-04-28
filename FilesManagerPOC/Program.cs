using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/file", async (context) =>
{
    var credentials = new BasicAWSCredentials("AKIAYV52FPUMHTGC4MVQ", "uYcUOpUrq6Ve8ViTUR5sQX7mLfkLt20xv1FCodUi");
    var config = new AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.SAEast1
    };
    var s3Client = new AmazonS3Client(credentials, config);

    var getObjectRequest = new GetObjectRequest
    {
        BucketName = "baup-file-manager",
        Key = "images/baup2.jpg"
    };

    using var response = await s3Client.GetObjectAsync(getObjectRequest);
    var stream = response.ResponseStream;

    // Redimensionar y bajar la calidad de la imagen
    using (var image = Image.Load(stream))
    {
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(800, 0),
            Mode = ResizeMode.Max
        }));

        var encoder = new JpegEncoder { Quality = 70 };

        using var output = new MemoryStream();
        image.Save(output, encoder);

        context.Response.ContentType = "image/jpeg";
        context.Response.StatusCode = 200;

        await context.Response.Body.WriteAsync(output.ToArray(), 0, (int)output.Length);
    }
})
.WithName("getFile");



app.MapPost("/file", async (IFormFile file) =>
{
    var credentials = new BasicAWSCredentials("AKIAYV52FPUMHTGC4MVQ", "uYcUOpUrq6Ve8ViTUR5sQX7mLfkLt20xv1FCodUi");
    var config = new AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.SAEast1
    };
    var s3Client = new AmazonS3Client(credentials, config);

    using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream);

    using var image = Image.Load(memoryStream.ToArray());

    var options = new ResizeOptions
    {
        Size = new Size(800, 600),
        Mode = ResizeMode.Max,
        Position = AnchorPositionMode.Center
    };
    image.Mutate(x => x.Resize(options));
    using var stream = new MemoryStream();
    image.Save(stream, new JpegEncoder
    {
        Quality = 60
    });

    stream.Seek(0, SeekOrigin.Begin);

    var putObjectRequest = new PutObjectRequest
    {
        BucketName = "baup-file-manager",
        Key = "images/" + file.FileName,
        ContentType = file.ContentType,
        InputStream = stream
    };

    await s3Client.PutObjectAsync(putObjectRequest);

})
.WithName("postFile");

app.MapDelete("/file", () =>
{

})
.WithName("deleteFile");

app.MapPut("/file", () =>
{

})
.WithName("updateFile");

app.Run();

