import time
import hashlib
import json
import asyncio
import logging
from typing import Any, Optional
from collections import OrderedDict

logger = logging.getLogger("EnjoyEveryday.AICache")

class InMemoryAICache:
    """
    Zero-dependency thread-safe LRU cache with TTL for AI generation requests.
    Prevents redundant calls to LLM providers for identical prompts, reducing latency and cost.
    """
    def __init__(self, max_size: int = 1000, default_ttl_seconds: int = 1800):
        self._max_size = max_size
        self._default_ttl = default_ttl_seconds
        self._cache: OrderedDict[str, tuple[float, Any]] = OrderedDict()
        self._lock = asyncio.Lock()

    @staticmethod
    def generate_key(tenant_id: str, namespace: str, payload: Any) -> str:
        """Computes a deterministic SHA-256 cache key."""
        if isinstance(payload, (dict, list)):
            serialized = json.dumps(payload, sort_keys=True, separators=(',', ':'))
        else:
            serialized = str(payload)
        raw_key = f"{tenant_id}:{namespace}:{serialized}"
        return hashlib.sha256(raw_key.encode("utf-8")).hexdigest()

    async def get(self, key: str) -> Optional[Any]:
        """Retrieves a cached value if present and not expired."""
        async with self._lock:
            if key not in self._cache:
                return None
            expiry, value = self._cache[key]
            if time.time() > expiry:
                del self._cache[key]
                return None
            # Move to end for LRU freshness
            self._cache.move_to_end(key)
            logger.info(f"AI Cache Hit [key={key[:8]}]")
            return value

    async def set(self, key: str, value: Any, ttl_seconds: Optional[int] = None) -> None:
        """Stores a value in the cache with expiration."""
        ttl = ttl_seconds if ttl_seconds is not None else self._default_ttl
        expiry = time.time() + ttl
        async with self._lock:
            if key in self._cache:
                self._cache.move_to_end(key)
            self._cache[key] = (expiry, value)
            if len(self._cache) > self._max_size:
                # Evict oldest item
                evicted_key, _ = self._cache.popitem(last=False)
                logger.debug(f"AI Cache evicted [key={evicted_key[:8]}]")

    async def clear(self) -> None:
        async with self._lock:
            self._cache.clear()

ai_cache = InMemoryAICache()
