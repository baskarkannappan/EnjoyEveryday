from fastapi import APIRouter, Depends, HTTPException
import json
import logging
from typing import List, Dict, Any
from app.schemas.requests import OrganizationIntelligenceRequest
from app.schemas.responses import OrganizationIntelligenceItem
from app.security.auth import get_tenant_context
from app.security.tenant_context import TenantContext
from app.providers.gemini_provider import gemini_provider
from app.prompts.organization_intelligence import ORGANIZATION_INTELLIGENCE_INSTRUCTION

logger = logging.getLogger("EnjoyEveryday.Api.OrganizationIntelligence")
router = APIRouter(tags=["Organization Intelligence"])

@router.post("/api/organization-intelligence", response_model=List[OrganizationIntelligenceItem])
async def generate_organization_intelligence(
    req: OrganizationIntelligenceRequest,
    context: TenantContext = Depends(get_tenant_context)
):
    def build_grounded_fallback() -> List[Dict[str, Any]]:
        insights = []
        # Insight 1: Operational Attendance & Activity
        att_obs = f"Attendance rate is currently {req.attendanceRate}%, with {req.completedExperiences} completed experiences and {req.upcomingExperiences} planned across {req.scopeName}."
        insights.append({
            "title": "Operational Rhythm & Attendance Health",
            "category": "OperationalOverview",
            "observation": att_obs,
            "possibleExplanation": "Stable attendance indicates predictable classroom routine and reliable family engagement.",
            "suggestedNextStep": "Continue reviewing morning arrival peaks to optimize staffing and classroom welcome moments.",
            "dataLimitations": "Data reflects records logged during the active reporting period."
        })

        # Insight 2: Curriculum Variety
        top_domain = max(req.domainBreakdown, key=req.domainBreakdown.get) if req.domainBreakdown else "Hands-On Play"
        var_obs = f"Classrooms engaged in {req.uniqueExperiences} distinct learning experiences with a diversity score of {req.varietyDiversityRate}%. The most active area is {top_domain}."
        insights.append({
            "title": "Curriculum Balance & Learning Domains",
            "category": "CurriculumVariety",
            "observation": var_obs,
            "possibleExplanation": "Educators have maintained an active repertoire with strong emphasis on creative exploration.",
            "suggestedNextStep": "Encourage teachers to incorporate more outdoor nature and movement activities into next week's schedule.",
            "dataLimitations": "Domain distribution is calculated based on planned experience tags."
        })

        # Insight 3: Planning Gaps
        if req.classroomsNeedingPlansCount > 0:
            cls_names = ", ".join(req.classroomsNeedingPlans[:3])
            gap_obs = f"{req.classroomsNeedingPlansCount} classroom(s) ({cls_names}) have fewer than 2 experiences scheduled for the upcoming week."
            insights.append({
                "title": "Advance Planning Readiness",
                "category": "PlanningGaps",
                "observation": gap_obs,
                "possibleExplanation": "Teachers may still be finalizing lesson choices or drafting routines manually.",
                "suggestedNextStep": "Prompt classroom lead educators to use the AI Weekly Planning Assistant to draft balanced 5-day schedules.",
                "dataLimitations": "Does not account for unrecorded informal daily play or seasonal closures."
            })

        # Insight 4: Data Quality & Operational Hygiene
        act_obs = f"Identified {req.missingDeparturesCount} departure checks without checkout timestamps and {req.pendingDraftsCount} parent communications awaiting review."
        insights.append({
            "title": "Operational Record Completeness",
            "category": "DataQuality",
            "observation": act_obs,
            "possibleExplanation": "Busy afternoon transitions can sometimes lead to deferred departure sign-outs or delayed draft approvals.",
            "suggestedNextStep": "Brief classroom teams during afternoon handover to ensure children are signed out and parent communications reviewed.",
            "dataLimitations": "Calculated based on attendance check-in timestamps and parent draft workflow statuses."
        })

        return insights

    prompt = f"Please provide executive-level organization intelligence insights based strictly on these metrics:\n{json.dumps(req.model_dump(), indent=2)}"

    try:
        raw_result = await gemini_provider.generate_json(
            prompt=prompt,
            system_instruction=ORGANIZATION_INTELLIGENCE_INSTRUCTION,
            tenant_context=context,
            temperature=0.3
        )
        if isinstance(raw_result, list) and len(raw_result) > 0:
            # Validate each item structure
            valid_list = []
            for item in raw_result:
                if isinstance(item, dict) and "title" in item and "category" in item and "observation" in item:
                    valid_list.append(item)
            if valid_list:
                return valid_list
    except Exception as ex:
        logger.warning(f"Organization intelligence AI failed: {ex}. Using grounded metrics fallback.")

    return build_grounded_fallback()
