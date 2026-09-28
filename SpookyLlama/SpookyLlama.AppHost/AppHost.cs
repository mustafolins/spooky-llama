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

var image = builder.AddUvicornApp(
		name: "image",
		appDirectory: "../SpookyLlamaImageService",
		app: "main:app")
	.WithUv()
	.WithHttpEndpoint(env: "PORT")
	.WithEnvironment("UVICORN_HOST", "0.0.0.0")
	.WithEnvironment("UVICORN_WORKERS", "1")
	.WithEnvironment("IMAGE_DEVICE", "cpu")
	.WithEnvironment("IMAGE_INFERENCE_STEPS", "8")
	.WithEnvironment(
		"HF_HOME",
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"spooky-llama",
			"models",
			"huggingface"))
	.WithHttpHealthCheck("/health");

builder.AddProject<Projects.SpookyLlamaBlazor>("web")
	.WithExternalHttpEndpoints()
	.WithHttpHealthCheck("/health")
	.WithReference(api)
	.WaitFor(api)
	.WithReference(speech)
	.WaitFor(speech)
	.WithReference(image)
	.WaitFor(image);

builder.Build().Run();
