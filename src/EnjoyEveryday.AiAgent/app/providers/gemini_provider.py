import json
import asyncio
import logging
from typing import Any, Optional, Type, List
from pydantic import BaseModel
from fastapi import HTTPException
from google import genai
from google.genai import types
from app.config import settings
from app.security.tenant_context import TenantContext
from app.security.rate_limiter import rate_limiter

logger = logging.getLogger("EnjoyEveryday.AIProvider")

class GeminiProvider:
    provider_id: str = "GoogleGemini"

    FALLBACK_MODELS: List[str] = [
        "gemini-flash-latest",
        "gemini-3-flash-preview",
        "gemini-2.5-flash-lite",
        "gemini-2.5-flash"
    ]

    def __init__(self):
        self._mock_handler = None
        self._working_model: Optional[str] = None

    def set_mock_handler(self, handler):
        """Allows injecting a deterministic mock for unit/integration tests."""
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
        # If mock handler is set (for testing without external network), use it
        if self._mock_handler:
            return await self._mock_handler(prompt, system_instruction, tenant_context, response_model)

        preferred_model = model_name or self._working_model or settings.DEFAULT_MODEL
        models_to_try = [preferred_model]
        for m in self.FALLBACK_MODELS:
            if m not in models_to_try:
                models_to_try.append(m)

        await rate_limiter.acquire_concurrency(tenant_context.tenant_id)
        client = genai.Client(api_key=tenant_context.gemini_api_key)

        last_error = None
        try:
            for current_model in models_to_try:
                try:
                    config = types.GenerateContentConfig(
                        system_instruction=system_instruction,
                        response_mime_type="application/json",
                        temperature=temperature,
                    )

                    response = await asyncio.wait_for(
                        client.aio.models.generate_content(
                            model=current_model,
                            contents=prompt,
                            config=config
                        ),
                        timeout=settings.DEFAULT_TIMEOUT_SECONDS
                    )

                    raw_text = response.text
                    if not raw_text or not raw_text.strip():
                        continue

                    clean_text = raw_text.strip()
                    if clean_text.startswith("```json"):
                        clean_text = clean_text[7:]
                    elif clean_text.startswith("```"):
                        clean_text = clean_text[3:]
                    if clean_text.endswith("```"):
                        clean_text = clean_text[:-3]
                    clean_text = clean_text.strip()

                    try:
                        data = json.loads(clean_text)
                    except json.JSONDecodeError as jde:
                        logger.error(f"Failed to decode JSON from Gemini ({current_model}): {jde}. Raw: {raw_text[:200]}")
                        continue

                    # Record working model for future requests
                    self._working_model = current_model
                    return data

                except asyncio.TimeoutError:
                    last_error = HTTPException(
                        status_code=504,
                        detail=f"AI generation timed out after {settings.DEFAULT_TIMEOUT_SECONDS} seconds."
                    )
                    break
                except Exception as model_err:
                    err_str = str(model_err)
                    logger.warning(f"Model {current_model} call failed: {err_str[:150]}")
                    
                    if "API_KEY_INVALID" in err_str or ("400" in err_str and "key" in err_str.lower()):
                        raise HTTPException(status_code=401, detail="Provided AI API key is invalid or not authorized.")
                    if "RESOURCE_EXHAUSTED" in err_str or "429" in err_str:
                        raise HTTPException(status_code=429, detail="AI Provider quota or rate limit exceeded.")
                    
                    last_error = model_err
                    # If 404 NOT_FOUND, 503 UNAVAILABLE, or temporary server error, continue loop to try next model in fallback list
                    retry_conditions = ["404", "NOT_FOUND", "not found", "503", "UNAVAILABLE", "unavailable", "500", "overloaded", "demand"]
                    if any(cond in err_str for cond in retry_conditions):
                        continue
                    else:
                        break

            # If all models failed, log available models from provider
            try:
                available = [m.name for m in client.models.list()]
                logger.info(f"Available models for this API key: {available}")
            except Exception as list_ex:
                logger.debug(f"Could not list models: {list_ex}")

            if isinstance(last_error, HTTPException):
                raise last_error

            raise HTTPException(
                status_code=502,
                detail=f"AI provider error across models: {last_error}"
            )

        finally:
            await rate_limiter.release_concurrency(tenant_context.tenant_id)

gemini_provider = GeminiProvider()
