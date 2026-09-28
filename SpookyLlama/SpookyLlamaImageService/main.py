from typing import Annotated

from fastapi import Depends, FastAPI
from fastapi.responses import JSONResponse, Response
from pydantic import BaseModel, Field, field_validator

from image_generator import DreamShaperGenerator, GeneratedImage, MODEL_ID


class ImageRequest(BaseModel):
    prompt: str = Field(max_length=500)
    seed: int | None = Field(default=None, ge=0, le=2**32 - 1)

    @field_validator("prompt")
    @classmethod
    def prompt_must_not_be_blank(cls, value: str) -> str:
        prompt = value.strip()
        if not prompt:
            raise ValueError("Prompt must not be blank.")
        return prompt


generator = DreamShaperGenerator()


def get_generator() -> DreamShaperGenerator:
    return generator


ImageGeneratorDependency = Annotated[DreamShaperGenerator, Depends(get_generator)]
app = FastAPI(title="Spooky Llama Image Service")


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "healthy"}


@app.get("/ready")
async def ready(image_generator: ImageGeneratorDependency):
    payload = {
        "ready": image_generator.is_ready,
        "model": MODEL_ID,
        "device": image_generator.device,
        "error": image_generator.load_error,
    }
    return JSONResponse(payload, status_code=200 if image_generator.is_ready else 503)


@app.post("/warmup", status_code=202)
async def warmup(image_generator: ImageGeneratorDependency) -> dict[str, str]:
    image_generator.start_loading()
    return {"status": "ready" if image_generator.is_ready else "loading"}


@app.post("/generate")
async def generate_image(
    request: ImageRequest,
    image_generator: ImageGeneratorDependency,
) -> Response:
    result: GeneratedImage = await image_generator.generate_async(
        request.prompt,
        request.seed,
    )
    return Response(
        content=result.data,
        media_type="image/png",
        headers={
            "X-Image-Model": MODEL_ID,
            "X-Image-Seed": str(result.seed),
            "X-Generation-Ms": str(result.latency_ms),
        },
    )