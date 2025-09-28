In order to run Ollama in a Docker container, you can use the following instructions.

Minimal CPU-Only Ollama Image

Pull the image:
`docker pull alpine/ollama`

Run the service:
`docker run -d -p 11434:11434 -v ~/.ollama:/root/.ollama --name ollama alpine/ollama`

Download a model (e.g., llama3.2):
`docker exec -ti ollama ollama pull llama3.2`

Test the API service:
```
curl http://localhost:11434/api/generate -d '{
"model": "llama3.2",
"prompt": "Why is the sky blue?"
}'
```