from fastapi import APIRouter
from .ideas import router as ideas_router
from .recommendations import router as recommendations_router
from .journey_insights import router as journey_insights_router
from .weekly_plan import router as weekly_plan_router
from .parent_communication import router as parent_communication_router
from .organization_intelligence import router as organization_intelligence_router

api_router = APIRouter()
api_router.include_router(ideas_router)
api_router.include_router(recommendations_router)
api_router.include_router(journey_insights_router)
api_router.include_router(weekly_plan_router)
api_router.include_router(parent_communication_router)
api_router.include_router(organization_intelligence_router)

__all__ = ["api_router"]
