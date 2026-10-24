from typing import Protocol, Any, Optional, Type, runtime_checkable
from pydantic import BaseModel
from app.security.tenant_context import TenantContext

@runtime_checkable
class AIProvider(Protocol):
    """
    Protocol for provider-independent text & structured JSON generation.
    Supports Google Gemini, OpenAI, Anthropic Claude, and Ollama.
    """
    provider_id: str

    async def generate_json(
        self,
        prompt: str,
        system_instruction: str,
        tenant_context: TenantContext,
        response_model: Optional[Type[BaseModel]] = None,
        model_name: Optional[str] = None,
        temperature: float = 0.4
    ) -> Any:
        ...

    def set_mock_handler(self, handler) -> None:
        ...
