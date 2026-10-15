from fastapi import APIRouter, Depends, HTTPException
import json
import logging
from typing import List, Dict, Any
from app.schemas.requests import ParentCommunicationRequest
from app.schemas.responses import ParentCommunicationResponse
from app.security.auth import get_tenant_context
from app.security.tenant_context import TenantContext
from app.providers.gemini_provider import gemini_provider
from app.prompts.parent_communication import PARENT_COMMUNICATION_INSTRUCTION

logger = logging.getLogger("EnjoyEveryday.Api.ParentCommunication")
router = APIRouter(tags=["Parent Communication"])

@router.post("/api/parent-communication", response_model=ParentCommunicationResponse)
async def generate_parent_communication(
    req: ParentCommunicationRequest,
    context: TenantContext = Depends(get_tenant_context)
):
    child_name = req.childName or "Your child"
    parent_greeting = f"Dear {req.recipientName}," if req.recipientName else f"Dear {child_name}'s Family,"

    def build_grounded_fallback():
        highlights_parts = []
        if req.attendanceSummary:
            highlights_parts.append(f"{child_name} was welcomed warmly into our classroom ({req.attendanceSummary}).")
        if req.completedExperiences:
            exp_text = "; ".join(req.completedExperiences[:3])
            highlights_parts.append(f"During group exploration, {child_name} participated with interest in: {exp_text}.")
        if req.approvedMoments:
            mom_text = "; ".join(req.approvedMoments[:3])
            highlights_parts.append(f"A wonderful moment we observed: {mom_text}.")
        if req.careHighlights:
            care_text = "; ".join(req.careHighlights[:3])
            highlights_parts.append(f"Care and daily rhythm highlights: {care_text}.")
        if not highlights_parts:
            highlights_parts.append(f"{child_name} had a calm, gentle time participating in classroom play and social interactions.")

        highlights = " ".join(highlights_parts)

        learning_parts = []
        if req.milestoneHighlights:
            learning_parts.append(f"Celebrated milestone: {'; '.join(req.milestoneHighlights)}.")
        if req.completedExperiences:
            learning_parts.append("These experiences supported hands-on discovery, social cooperation, and fine-motor expression.")
        else:
            learning_parts.append("Today's play supported communication, peer relationships, and natural curiosity.")
        growth = " ".join(learning_parts)

        starter = f"Ask {child_name} about their favorite thing they explored or played with friends today!"
        if req.completedExperiences:
            first_exp = req.completedExperiences[0].split(":")[0]
            starter = f"Ask {child_name} what they enjoyed creating during our '{first_exp}' exploration today!"

        title = f"{child_name}'s Daily Joy & Learning" if req.communicationType == "DailyUpdate" else f"{child_name}'s Weekly Learning Highlights"

        return {
            "title": title,
            "greeting": parent_greeting,
            "highlights": highlights,
            "growthAndLearning": growth,
            "familyConversationStarter": starter,
            "signOff": f"Warmly,\nThe {req.classroomName} Team"
        }

    context_payload = {
        "childName": child_name,
        "classroomName": req.classroomName,
        "recipientName": req.recipientName,
        "recipientRelationship": req.recipientRelationship,
        "communicationType": req.communicationType,
        "reportingPeriod": req.reportingPeriod,
        "attendanceSummary": req.attendanceSummary,
        "completedExperiences": req.completedExperiences[:5],
        "approvedMoments": req.approvedMoments[:5],
        "careHighlights": req.careHighlights[:5],
        "milestoneHighlights": req.milestoneHighlights[:3],
        "relevantEvents": req.relevantEvents[:3],
        "teacherNotes": req.teacherNotes
    }

    prompt = f"Please prepare a parent communication draft based strictly on this verified evidence:\n{json.dumps(context_payload, indent=2)}"

    try:
        raw_result = await gemini_provider.generate_json(
            prompt=prompt,
            system_instruction=PARENT_COMMUNICATION_INSTRUCTION,
            tenant_context=context,
            temperature=0.3
        )
        if isinstance(raw_result, dict) and "highlights" in raw_result:
            return {
                "title": raw_result.get("title", f"{child_name}'s Learning Update"),
                "greeting": raw_result.get("greeting", parent_greeting),
                "highlights": raw_result.get("highlights", ""),
                "growthAndLearning": raw_result.get("growthAndLearning", ""),
                "familyConversationStarter": raw_result.get("familyConversationStarter", ""),
                "signOff": raw_result.get("signOff", f"Warmly,\nThe {req.classroomName} Team")
            }
    except Exception as ex:
        logger.warning(f"Parent communication AI call failed: {ex}. Using grounded fallback.")

    return build_grounded_fallback()
