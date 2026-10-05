import os
from functools import lru_cache
from typing import Annotated
from typing import Protocol

from fastapi import Depends, FastAPI
from fastapi.responses import JSONResponse, Response
from pydantic import BaseModel, Field, field_validator
from redis.asyncio import Redis

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


class LatestImageStore(Protocol):
    async def set(self, image: GeneratedImage) -> None: ...

    async def get(self) -> GeneratedImage | None: ...


class RedisLatestImageStore:
    _key = "spooky-llama:media:image:latest"

    def __init__(self, redis: Redis) -> None:
        self._redis = redis

    async def set(self, image: GeneratedImage) -> None:
        await self._redis.hset(
            self._key,
            mapping={
                "data": image.data,
                "seed": str(image.seed),
                "latency_ms": str(image.latency_ms),
            },
        )

    async def get(self) -> GeneratedImage | None:
        fields = await self._redis.hgetall(self._key)
        if not fields:
            return None

        return GeneratedImage(
            fields[b"data"],
            int(fields[b"seed"]),
            float(fields[b"latency_ms"]),
        )


generator = DreamShaperGenerator()


def get_generator() -> DreamShaperGenerator:
    return generator


@lru_cache
def get_latest_image_store() -> LatestImageStore:
    redis_uri = os.environ.get("CACHE_URI")
    if not redis_uri:
        raise RuntimeError("The Aspire Redis cache endpoint is not configured.")

    return RedisLatestImageStore(Redis.from_url(redis_uri))


ImageGeneratorDependency = Annotated[DreamShaperGenerator, Depends(get_generator)]
LatestImageStoreDependency = Annotated[
    LatestImageStore,
    Depends(get_latest_image_store),
]
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
    latest_images: LatestImageStoreDependency,
) -> Response:
    result: GeneratedImage = await image_generator.generate_async(
        request.prompt,
        request.seed,
    )
    await latest_images.set(result)
    return Response(
        content=result.data,
        media_type="image/png",
        headers={
            "X-Image-Model": MODEL_ID,
            "X-Image-Seed": str(result.seed),
            "X-Generation-Ms": str(result.latency_ms),
        },
    )


@app.get("/generate/latest")
async def get_latest_image(latest_images: LatestImageStoreDependency) -> Response:
    image = await latest_images.get()
    if image is None:
        return Response(status_code=404)

    return Response(
        content=image.data,
        media_type="image/png",
        headers={
            "X-Image-Model": MODEL_ID,
            "X-Image-Seed": str(image.seed),
            "X-Generation-Ms": str(image.latency_ms),
        },
    )