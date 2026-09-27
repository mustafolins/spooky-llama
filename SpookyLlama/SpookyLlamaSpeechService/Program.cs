using SpookyLlamaSpeechService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddHttpClient("kokoro-model");
builder.Services.AddSingleton<ISpeechSynthesizer, KokoroSpeechSynthesizer>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapPost("/speech", async Task<IResult> (
	SpeechRequest request,
	ISpeechSynthesizer speechSynthesizer,
	CancellationToken cancellationToken) =>
{
	if (string.IsNullOrWhiteSpace(request.Text))
	{
		return Results.BadRequest();
	}

	var audio = await speechSynthesizer.SynthesizeAsync(request.Text, cancellationToken);
	return Results.File(audio, "audio/wav", enableRangeProcessing: true);
})
	.WithName("SynthesizeSpeech");

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
