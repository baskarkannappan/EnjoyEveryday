import os
from dataclasses import dataclass
from typing import Optional

@dataclass(frozen=True)
class Settings:
    APP_NAME: str = "EnjoyEveryday AI Intelligence Engine"
    APP_VERSION: str = "2.0.0"
    DEBUG: bool = os.getenv("DEBUG", "false").lower() == "true"

    # Gemini AI configuration
    DEFAULT_MODEL: str = os.getenv("GEMINI_MODEL", "gemini-flash-latest")
    DEFAULT_TIMEOUT_SECONDS: int = int(os.getenv("GEMINI_TIMEOUT_SECONDS", "60"))
    MAX_RETRIES: int = int(os.getenv("GEMINI_MAX_RETRIES", "2"))

    # Server-to-server security
    SERVICE_SECRET: Optional[str] = os.getenv("AI_SERVICE_SECRET", None)
    REQUIRE_SERVICE_AUTH: bool = os.getenv("REQUIRE_SERVICE_AUTH", "false").lower() == "true"

    # Rate limiting & bounds
    RATE_LIMIT_PER_MINUTE: int = int(os.getenv("RATE_LIMIT_PER_MINUTE", "60"))
    MAX_CONCURRENT_GENERATIONS: int = int(os.getenv("MAX_CONCURRENT_GENERATIONS", "10"))
    MAX_CANDIDATE_EXPERIENCES: int = 30
    MAX_EVIDENCE_MEMORIES: int = 40
    MAX_TEXT_INPUT_LENGTH: int = 10000

settings = Settings()
