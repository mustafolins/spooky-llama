# Spooky Llama

Spooky Llama is an Aspire application that combines a Blazor WebAssembly client, an ASP.NET Core API, a local Ollama model, and Kokoro text-to-speech.

## Prerequisites

- .NET 10 SDK
- Aspire CLI 13.5 or newer
- Docker or Podman

## Run locally

From the repository root:

```sh
cd SpookyLlama/SpookyLlama.AppHost
aspire run
```

Open the `app` endpoint from the Aspire dashboard. The first run pulls the Ollama image and the `llama3.2` model, so it can take a few minutes. Ollama model data is stored in a persistent container volume and reused on later runs.

The first spoken response also downloads the Kokoro model to `~/.local/share/spooky-llama/models/kokoro.onnx` on Linux. That model is reused across Aspire restarts.

The AppHost starts these resources:

- `ollama`: the Ollama server
- `llama`: the `llama3.2` model
- `api`: the Spooky Llama chat API
- `speech`: the Kokoro WAV synthesis service
- `app`: the Blazor WebAssembly client
- `web`: the browser-facing Blazor gateway

## Build

```sh
dotnet build SpookyLlama/SpookyLlama.sln
dotnet test SpookyLlama/SpookyLlamaSpeechService.Tests/SpookyLlamaSpeechService.Tests.csproj
```

For background lifecycle management:

```sh
cd SpookyLlama/SpookyLlama.AppHost
aspire start
aspire describe
aspire stop
```

The console project still supports an independently running Ollama instance at `http://localhost:11434`.