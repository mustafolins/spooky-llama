var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
	.WithDataVolume();

var ollama = builder.AddOllama("ollama")
	.WithGPUSupport()
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
	.WithReference(cache)
	.WaitFor(cache)
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
	.WithReference(cache)
	.WaitFor(cache)
	.WithHttpHealthCheck("/health");

var video = builder.AddProject<Projects.SpookyLlamaVideoService>("video")
	.WithReference(speech)
	.WaitFor(speech)
	.WithReference(image)
	.WaitFor(image)
	.WithHttpHealthCheck("/health");

builder.AddProject<Projects.SpookyLlamaBlazor>("web")
	.WithExternalHttpEndpoints()
	.WithHttpHealthCheck("/health")
	.WithReference(api)
	.WaitFor(api)
	.WithReference(speech)
	.WaitFor(speech)
	.WithReference(image)
	.WaitFor(image)
	.WithReference(video)
	.WaitFor(video);

builder.Build().Run();
