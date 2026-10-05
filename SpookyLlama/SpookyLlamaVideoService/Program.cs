using SpookyLlamaVideoService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHttpClient("speech", client =>
{
    client.BaseAddress = new Uri("https+http://speech");
    client.Timeout = TimeSpan.FromMinutes(3);
})
    .AddStandardResilienceHandler();
builder.Services.AddHttpClient("image", client =>
{
    client.BaseAddress = new Uri("https+http://image");
    client.Timeout = TimeSpan.FromMinutes(3);
})
    .AddStandardResilienceHandler();
builder.Services.AddSingleton<IVideoComposer, FFmpegVideoComposer>();
builder.Services.AddSingleton<IVideoService, VideoService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapPost("/video", async Task<IResult> (
    IVideoService videoService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var video = await videoService.GenerateVideoAsync(cancellationToken);
        return Results.File(video, "video/mp4", enableRangeProcessing: true);
    }
    catch (LatestMediaUnavailableException exception)
    {
        return Results.Problem(
            title: "Generated media is unavailable",
            detail: exception.Message,
            statusCode: StatusCodes.Status424FailedDependency);
    }
})
    .WithName("GenerateVideo")
    .Produces(StatusCodes.Status200OK, contentType: "video/mp4")
    .ProducesProblem(StatusCodes.Status424FailedDependency);

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
