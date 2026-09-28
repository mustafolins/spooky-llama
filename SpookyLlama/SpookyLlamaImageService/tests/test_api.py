import unittest

import httpx

from image_generator import GeneratedImage
from main import app, get_generator


PNG_BYTES = b"\x89PNG\r\n\x1a\nimage"


class FakeGenerator:
    def __init__(self) -> None:
        self.is_ready = False
        self.load_error: str | None = None
        self.device = "cpu"
        self.last_prompt: str | None = None
        self.warmup_started = False

    def start_loading(self) -> None:
        self.warmup_started = True

    async def generate_async(
        self,
        prompt: str,
        seed: int | None = None,
    ) -> GeneratedImage:
        self.last_prompt = prompt
        return GeneratedImage(PNG_BYTES, 33, 125.4)


class ImageApiTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self) -> None:
        self.generator = FakeGenerator()
        app.dependency_overrides[get_generator] = lambda: self.generator
        self.client = httpx.AsyncClient(
            transport=httpx.ASGITransport(app=app),
            base_url="http://test",
        )

    async def asyncTearDown(self) -> None:
        app.dependency_overrides.clear()
        await self.client.aclose()

    async def test_health_does_not_require_loaded_model(self) -> None:
        response = await self.client.get("/health")

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json(), {"status": "healthy"})

    async def test_ready_reports_unavailable_before_warmup(self) -> None:
        response = await self.client.get("/ready")

        self.assertEqual(response.status_code, 503)
        self.assertFalse(response.json()["ready"])

    async def test_warmup_starts_model_loading(self) -> None:
        response = await self.client.post("/warmup")

        self.assertEqual(response.status_code, 202)
        self.assertTrue(self.generator.warmup_started)

    async def test_generate_rejects_blank_prompt(self) -> None:
        response = await self.client.post("/generate", json={"prompt": "   "})

        self.assertEqual(response.status_code, 422)

    async def test_generate_returns_png_with_metadata(self) -> None:
        response = await self.client.post(
            "/generate",
            json={"prompt": "A moonlit figure emerging from the fog", "seed": 33},
        )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.headers["content-type"], "image/png")
        self.assertEqual(response.headers["x-image-model"], "Lykon/dreamshaper-8")
        self.assertEqual(response.headers["x-image-seed"], "33")
        self.assertEqual(response.content, PNG_BYTES)
        self.assertEqual(
            self.generator.last_prompt,
            "A moonlit figure emerging from the fog",
        )


if __name__ == "__main__":
    unittest.main()