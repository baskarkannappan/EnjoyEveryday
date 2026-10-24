import logging
from typing import Dict, Optional
from app.config import settings
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

    @property
    def default(self) -> AIProvider:
        return self.get_provider(self._default_provider_id)

ai_provider_registry = AIProviderRegistry()
