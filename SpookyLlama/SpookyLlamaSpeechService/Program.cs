using SpookyLlamaSpeechService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddHttpClient("kokoro-model")
	.AddStandardResilienceHandler();
builder.AddRedisDistributedCache("cache");
builder.Services.AddSingleton<ISpeechSynthesizer, KokoroSpeechSynthesizer>();
builder.Services.AddSingleton<LatestSpeechStore>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapPost("/speech", async Task<IResult> (
	SpeechRequest request,
	ISpeechSynthesizer speechSynthesizer,
	LatestSpeechStore latestSpeech,
	CancellationToken cancellationToken) =>
{
	if (string.IsNullOrWhiteSpace(request.Text))
	{
		return Results.BadRequest();
	}

	var audio = await speechSynthesizer.SynthesizeAsync(request.Text, cancellationToken);
	await latestSpeech.SetAsync(audio, cancellationToken);
	return Results.File(audio, "audio/wav", enableRangeProcessing: true);
})
	.WithName("SynthesizeSpeech");

app.MapGet("/speech/latest", async Task<IResult> (
	LatestSpeechStore latestSpeech,
	CancellationToken cancellationToken) =>
{
	var audio = await latestSpeech.GetAsync(cancellationToken);
	return audio is null
		? Results.NotFound()
		: Results.File(audio, "audio/wav", enableRangeProcessing: true);
})
	.WithName("GetLatestSpeech");

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
