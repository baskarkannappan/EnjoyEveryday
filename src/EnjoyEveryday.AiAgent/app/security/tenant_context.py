from dataclasses import dataclass
from typing import Optional
import uuid

@dataclass
class TenantContext:
    tenant_id: str
    gemini_api_key: str
    caller_service: Optional[str] = None
    correlation_id: str = ""

    def __post_init__(self):
        if not self.correlation_id:
            self.correlation_id = str(uuid.uuid4())
