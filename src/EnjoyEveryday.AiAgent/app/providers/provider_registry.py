import logging
from typing import Dict, Optional, Any, Type
from pydantic import BaseModel
from app.config import settings
from app.security.tenant_context import TenantContext
from app.providers.base_provider import AIProvider
from app.providers.gemini_provider import GeminiProvider
from app.providers.openai_provider import OpenAIProvider

logger = logging.getLogger("EnjoyEveryday.AIProviderRegistry")

class AIProviderRegistry:
    """
    Central registry for managing configured AI generation providers.
    Allows runtime selection and decouples business logic endpoints from specific vendors.
    """
    def __init__(self):
        self._providers: Dict[str, AIProvider] = {}
        # Register standard default Gemini and OpenAI providers
        self.register_provider(GeminiProvider())
        self.register_provider(OpenAIProvider())
        
        # Configure default
        configured = settings.DEFAULT_PROVIDER
        if configured in self._providers:
            self._default_provider_id = configured
        else:
            self._default_provider_id = "GoogleGemini"

    def register_provider(self, provider: AIProvider) -> None:
        self._providers[provider.provider_id] = provider
        logger.info(f"Registered AI provider: {provider.provider_id}")

    def get_provider(self, provider_id: Optional[str] = None) -> AIProvider:
        pid = provider_id or self._default_provider_id
        if pid in self._providers:
            return self._providers[pid]
        raise ValueError(f"AI provider '{pid}' is not registered or supported.")

    async def generate_with_failover(
        self,
        prompt: str,
        system_instruction: str,
        tenant_context: TenantContext,
        response_model: Optional[Type[BaseModel]] = None,
        temperature: float = 0.4
    ) -> Any:
        """
        Attempts generation with primary provider. If all retries/models fail,
        automatically falls over to OpenAI before giving up to safe templates.
        """
        primary = self.get_provider(self._default_provider_id)
        try:
            return await primary.generate_json(
                prompt=prompt,
                system_instruction=system_instruction,
                tenant_context=tenant_context,
                response_model=response_model,
                temperature=temperature
            )
        except Exception as primary_error:
            logger.warning(f"Primary AI provider '{primary.provider_id}' failed: {primary_error}")

            # Check if OpenAI is available as a fallback
            openai_key = tenant_context.openai_api_key or settings.OPENAI_API_KEY
            if openai_key and primary.provider_id != "OpenAI":
                logger.info("Failing over to OpenAI provider (gpt-4o-mini)...")
                try:
                    openai_prov = self.get_provider("OpenAI")
                    return await openai_prov.generate_json(
                        prompt=prompt,
                        system_instruction=system_instruction,
                        tenant_context=tenant_context,
                        response_model=response_model,
                        temperature=temperature
                    )
                except Exception as secondary_error:
                    logger.warning(f"OpenAI fallback provider also failed: {secondary_error}")

            raise primary_error

    @property
    def default(self) -> AIProvider:
        return self.get_provider(self._default_provider_id)

ai_provider_registry = AIProviderRegistry()
