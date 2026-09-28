# Spooky Llama

Spooky Llama is an Aspire application that combines an Interactive Server Blazor Web App, an ASP.NET Core API, a local Ollama model, Kokoro text-to-speech, and DreamShaper image generation.

## Prerequisites

- .NET 10 SDK
- Aspire CLI 13.5 or newer
- Docker or Podman
- Python 3.12
- `uv`

## Run locally

From the repository root:

```sh
cd SpookyLlama/SpookyLlama.AppHost
aspire run
```

Open the `web` endpoint from the Aspire dashboard. The first run pulls the Ollama image and the `llama3.2` model, so it can take a few minutes. Ollama model data is stored in a persistent container volume and reused on later runs.

The first spoken response also downloads the Kokoro model to `~/.local/share/spooky-llama/models/kokoro.onnx` on Linux. That model is reused across Aspire restarts.

The first generated image downloads about 5.2 GB of DreamShaper model data to `~/.local/share/spooky-llama/models/huggingface`. The checked-in profile uses eight inference steps and CPU-only PyTorch for portability, so image generation can take over a minute. DreamShaper 8 is distributed under the [CreativeML Open RAIL-M license](https://huggingface.co/Lykon/dreamshaper-8).

The AppHost starts these resources:

- `ollama`: the Ollama server
- `llama`: the `llama3.2` model
- `api`: the Spooky Llama chat API
- `speech`: the Kokoro WAV synthesis service
- `image`: the Python DreamShaper image-generation service
- `web`: the Interactive Server Blazor application

## Build

```sh
dotnet build SpookyLlama/SpookyLlama.sln
dotnet test SpookyLlama/SpookyLlamaSpeechService.Tests/SpookyLlamaSpeechService.Tests.csproj
cd SpookyLlama/SpookyLlamaImageService
uv run python -m unittest discover -s tests
```

For background lifecycle management:

```sh
cd SpookyLlama/SpookyLlama.AppHost
aspire start
aspire describe
aspire stop
```

The console project still supports an independently running Ollama instance at `http://localhost:11434`.