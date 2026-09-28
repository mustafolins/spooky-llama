import asyncio
import io
import logging
import os
import secrets
import time
from dataclasses import dataclass
from typing import Any


MODEL_ID = "Lykon/dreamshaper-8"
IMAGE_SIZE = 512
DEFAULT_NEGATIVE_PROMPT = (
    "text, watermark, signature, blurry, low quality, distorted, deformed"
)

logger = logging.getLogger(__name__)


@dataclass(frozen=True, slots=True)
class GeneratedImage:
    data: bytes
    seed: int
    latency_ms: float


class DreamShaperGenerator:
    def __init__(self) -> None:
        self._pipeline: Any | None = None
        self._torch: Any | None = None
        self._device = "cpu"
        self._load_lock = asyncio.Lock()
        self._inference_lock = asyncio.Lock()
        self._load_task: asyncio.Task[None] | None = None
        self._load_error: str | None = None

    @property
    def is_ready(self) -> bool:
        return self._pipeline is not None

    @property
    def load_error(self) -> str | None:
        return self._load_error

    @property
    def device(self) -> str:
        return self._device

    def start_loading(self) -> None:
        if self._load_task is None or self._load_task.done():
            self._load_task = asyncio.create_task(self._load_in_background())

    async def load_async(self) -> None:
        if self.is_ready:
            return

        async with self._load_lock:
            if self.is_ready:
                return

            try:
                await asyncio.to_thread(self._load_sync)
                self._load_error = None
            except Exception as error:
                self._load_error = str(error)
                logger.exception("Failed to load DreamShaper")
                raise

    async def generate_async(
        self,
        prompt: str,
        seed: int | None = None,
    ) -> GeneratedImage:
        await self.load_async()
        selected_seed = seed if seed is not None else secrets.randbelow(2**32)
        started_at = time.perf_counter()

        async with self._inference_lock:
            image_bytes = await asyncio.to_thread(
                self._generate_sync,
                prompt,
                selected_seed,
            )

        return GeneratedImage(
            data=image_bytes,
            seed=selected_seed,
            latency_ms=round((time.perf_counter() - started_at) * 1000, 1),
        )

    async def _load_in_background(self) -> None:
        try:
            await self.load_async()
        except Exception:
            pass

    def _load_sync(self) -> None:
        import torch
        from diffusers import AutoPipelineForText2Image, DEISMultistepScheduler

        requested_device = os.getenv("IMAGE_DEVICE", "auto").lower()
        use_cuda = requested_device != "cpu" and torch.cuda.is_available()
        if requested_device == "cuda" and not use_cuda:
            logger.warning("CUDA was requested but is unavailable; falling back to CPU.")

        self._device = "cuda" if use_cuda else "cpu"
        torch_dtype = torch.float16 if use_cuda else torch.float32
        load_options: dict[str, object] = {
            "torch_dtype": torch_dtype,
            "use_safetensors": True,
        }
        if use_cuda:
            load_options["variant"] = "fp16"

        logger.info("Loading %s on %s", MODEL_ID, self._device)
        pipeline = AutoPipelineForText2Image.from_pretrained(MODEL_ID, **load_options)
        pipeline.scheduler = DEISMultistepScheduler.from_config(pipeline.scheduler.config)
        pipeline.enable_attention_slicing()
        pipeline.vae.enable_slicing()

        use_low_vram = os.getenv("IMAGE_LOW_VRAM", "true").lower() == "true"
        if use_cuda and use_low_vram:
            pipeline.enable_model_cpu_offload()
        else:
            pipeline.to(self._device)

        pipeline.set_progress_bar_config(disable=True)
        self._torch = torch
        self._pipeline = pipeline
        logger.info("DreamShaper ready")

    def _generate_sync(self, prompt: str, seed: int) -> bytes:
        steps = int(os.getenv("IMAGE_INFERENCE_STEPS", "25"))
        styled_prompt = (
            "cinematic horror illustration, atmospheric, dramatic lighting, "
            f"high detail, {prompt}"
        )
        random_generator = self._torch.Generator(device=self._device).manual_seed(seed)

        with self._torch.inference_mode():
            image = self._pipeline(
                prompt=styled_prompt,
                negative_prompt=DEFAULT_NEGATIVE_PROMPT,
                width=IMAGE_SIZE,
                height=IMAGE_SIZE,
                num_inference_steps=steps,
                guidance_scale=7.0,
                generator=random_generator,
            ).images[0]

        image_stream = io.BytesIO()
        image.save(image_stream, format="PNG", optimize=True)
        return image_stream.getvalue()