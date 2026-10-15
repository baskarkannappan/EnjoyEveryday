from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
import time
import uuid
import logging
from app.config import settings
from app.api import api_router

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] [%(name)s] %(message)s"
)
logger = logging.getLogger("EnjoyEveryday.AiAgent")

app = FastAPI(
    title=settings.APP_NAME,
    version=settings.APP_VERSION,
    description="Production-ready early childhood education AI intelligence engine."
)

# CORS middleware for local development and .NET microservices
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.middleware("http")
async def correlation_and_metrics_middleware(request: Request, call_next):
    correlation_id = request.headers.get("X-Correlation-Id") or str(uuid.uuid4())
    start_time = time.time()

    response = await call_next(request)

    duration_ms = round((time.time() - start_time) * 1000, 2)
    response.headers["X-Correlation-Id"] = correlation_id
    response.headers["X-Response-Time-Ms"] = str(duration_ms)

    logger.info(f"{request.method} {request.url.path} -> {response.status_code} ({duration_ms}ms) [cid={correlation_id}]")
    return response

@app.get("/health")
async def health_check():
    return {
        "status": "healthy",
        "app": settings.APP_NAME,
        "version": settings.APP_VERSION,
        "defaultModel": settings.DEFAULT_MODEL
    }

# Register all modular routes
app.include_router(api_router)
