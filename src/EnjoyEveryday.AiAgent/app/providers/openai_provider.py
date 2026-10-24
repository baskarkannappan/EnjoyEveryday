import json
import logging
from typing import Any, Optional, Type, List
from pydantic import BaseModel
from fastapi import HTTPException
import httpx
from app.config import settings
from app.security.tenant_context import TenantContext
from app.security.rate_limiter import rate_limiter

logger = logging.getLogger("EnjoyEveryday.OpenAIProvider")

class OpenAIProvider:
    provider_id: str = "OpenAI"

    FALLBACK_MODELS: List[str] = [
        "gpt-4o-mini",
        "gpt-4o"
    ]

    def __init__(self):
        self._mock_handler = None
        self._working_model: Optional[str] = None

    def set_mock_handler(self, handler):
        self._mock_handler = handler

    async def generate_json(
        self,
        prompt: str,
        system_instruction: str,
        tenant_context: TenantContext,
        response_model: Optional[Type[BaseModel]] = None,
        model_name: Optional[str] = None,
        temperature: float = 0.4
    ) -> Any:
        if self._mock_handler:
            return await self._mock_handler(prompt, system_instruction, tenant_context, response_model)

        preferred_model = model_name or self._working_model or "gpt-4o-mini"
        await rate_limiter.acquire_concurrency(tenant_context.tenant_id)

        try:
            headers = {
                "Authorization": f"Bearer {tenant_context.gemini_api_key}",
                "Content-Type": "application/json"
            }
            payload = {
                "model": preferred_model,
                "messages": [
                    {"role": "system", "content": system_instruction},
                    {"role": "user", "content": prompt}
                ],
                "response_format": {"type": "json_object"},
                "temperature": temperature
            }

            async with httpx.AsyncClient(timeout=settings.DEFAULT_TIMEOUT_SECONDS) as client:
                response = await client.post("https://api.openai.com/v1/chat/completions", json=payload, headers=headers)
                
                if response.status_code == 401:
                    raise HTTPException(status_code=401, detail="OpenAI API key is invalid or unauthorized.")
                elif response.status_code == 429:
                    raise HTTPException(status_code=429, detail="OpenAI rate limit exceeded.")
                elif response.status_code >= 400:
                    raise HTTPException(status_code=502, detail=f"OpenAI error: {response.text}")

                res_json = response.json()
                raw_text = res_json["choices"][0]["message"]["content"]
                return json.loads(raw_text)
        except json.JSONDecodeError as jde:
            logger.error(f"Failed to decode JSON from OpenAI: {jde}")
            raise HTTPException(status_code=502, detail="Invalid JSON returned from OpenAI.")
        finally:
            await rate_limiter.release_concurrency(tenant_context.tenant_id)

openai_provider = OpenAIProvider()
