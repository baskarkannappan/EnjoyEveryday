import time
import asyncio
from collections import defaultdict
from fastapi import HTTPException
from app.config import settings

class RateLimiter:
    def __init__(self):
        # tenant_id -> list of timestamps
        self._history = defaultdict(list)
        # tenant_id -> active concurrent count
        self._active_counts = defaultdict(int)
        self._lock = asyncio.Lock()
        self._global_semaphore = asyncio.Semaphore(settings.MAX_CONCURRENT_GENERATIONS)

    async def check_and_record(self, tenant_id: str):
        now = time.time()
        window_start = now - 60.0

        async with self._lock:
            # Clean up old timestamps
            self._history[tenant_id] = [t for t in self._history[tenant_id] if t > window_start]

            if len(self._history[tenant_id]) >= settings.RATE_LIMIT_PER_MINUTE:
                raise HTTPException(
                    status_code=429,
                    detail=f"Rate limit exceeded for tenant. Maximum {settings.RATE_LIMIT_PER_MINUTE} requests per minute allowed."
                )

            self._history[tenant_id].append(now)

    async def acquire_concurrency(self, tenant_id: str):
        await self._global_semaphore.acquire()
        async with self._lock:
            self._active_counts[tenant_id] += 1

    async def release_concurrency(self, tenant_id: str):
        async with self._lock:
            self._active_counts[tenant_id] = max(0, self._active_counts[tenant_id] - 1)
        self._global_semaphore.release()

rate_limiter = RateLimiter()
