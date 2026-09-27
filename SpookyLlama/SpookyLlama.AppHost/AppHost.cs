var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
	.WithDataVolume();

var llama = ollama.AddModel("llama", "llama3.2");

var api = builder.AddProject<Projects.SpookyLlamaApi>("api")
	.WithReference(llama)
	.WaitFor(llama)
	.WithHttpHealthCheck("/health");

var speech = builder.AddProject<Projects.SpookyLlamaSpeechService>("speech")
	.WithEnvironment(
		"Speech__Kokoro__ModelPath",
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"spooky-llama",
			"models",
			"kokoro.onnx"))
	.WithHttpHealthCheck("/health");

var client = builder.AddBlazorWasmProject<Projects.SpookyLlamaBlazor>("app")
	.WithReference(api)
	.WithReference(speech);

builder.AddBlazorGateway("web")
	.WithExternalHttpEndpoints()
	.WithOtlpExporter(OtlpProtocol.HttpProtobuf)
	.WithBlazorClientApp(client)
	.WaitFor(api)
	.WaitFor(speech);

builder.Build().Run();
