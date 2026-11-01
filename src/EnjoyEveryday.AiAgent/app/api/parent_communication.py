from fastapi import APIRouter, Depends, HTTPException
import json
import logging
from typing import List, Dict, Any
from app.schemas.requests import ParentCommunicationRequest
from app.schemas.responses import ParentCommunicationResponse
from app.security.auth import get_tenant_context
from app.security.tenant_context import TenantContext
from app.providers.provider_registry import ai_provider_registry
from app.prompts.parent_communication import PARENT_COMMUNICATION_INSTRUCTION
from app.cache.ai_cache import ai_cache

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
            moments_text = "; ".join(req.approvedMoments[:2])
            highlights_parts.append(f"Special moments observed today included: {moments_text}.")
        if req.careHighlights:
            care_text = "; ".join(req.careHighlights[:2])
            highlights_parts.append(f"Daily care routine: {care_text}.")
        if req.teacherNotes:
            highlights_parts.append(f"Educator note: {req.teacherNotes}.")

        growth = f"{child_name} demonstrated curiosity, positive engagement with classroom materials, and collaborative play."
        starter = f"At home this evening, you might ask {child_name} to share their favorite part of today's classroom activities!"
        if req.completedExperiences:
            first_exp = req.completedExperiences[0]
            starter = f"Ask {child_name} about '{first_exp}' and what they enjoyed creating or discovering!"

        return {
            "title": f"{child_name}'s Daily Learning & Care Update",
            "greeting": parent_greeting,
            "highlights": " ".join(highlights_parts) or f"{child_name} had a calm and engaging day exploring classroom learning centers.",
            "growthAndLearning": growth,
            "familyConversationStarter": starter,
            "signOff": f"Warmly,\nThe {req.classroomName} Team"
        }

    cache_key = ai_cache.generate_key(context.tenant_id, "parent_comm", req.model_dump())
    cached_comm = await ai_cache.get(cache_key)
    if cached_comm is not None:
        return cached_comm

    context_payload = {
        "childName": child_name,
        "classroomName": req.classroomName,
        "date": req.date,
        "recipientName": req.recipientName,
        "attendanceSummary": req.attendanceSummary,
        "completedExperiences": req.completedExperiences[:5],
        "approvedMoments": req.approvedMoments[:5],
        "careHighlights": req.careHighlights[:5],
        "milestoneHighlights": req.milestoneHighlights[:3],
        "relevantEvents": req.relevantEvents[:3],
        "teacherNotes": req.teacherNotes
    }

    compact_payload = json.dumps(context_payload, separators=(',', ':'))
    prompt = f"Please prepare a parent communication draft based strictly on this verified evidence:\n{compact_payload}"
    provider = ai_provider_registry.get_provider()

    try:
        raw_result = await provider.generate_json(
            prompt=prompt,
            system_instruction=PARENT_COMMUNICATION_INSTRUCTION,
            tenant_context=context,
            temperature=0.3
        )
        if isinstance(raw_result, dict) and "highlights" in raw_result:
            result = {
                "title": raw_result.get("title", f"{child_name}'s Learning Update"),
                "greeting": raw_result.get("greeting", parent_greeting),
                "highlights": raw_result.get("highlights", ""),
                "growthAndLearning": raw_result.get("growthAndLearning", ""),
                "familyConversationStarter": raw_result.get("familyConversationStarter", ""),
                "signOff": raw_result.get("signOff", f"Warmly,\nThe {req.classroomName} Team")
            }
            await ai_cache.set(cache_key, result, ttl_seconds=1800)
            return result
    except Exception as ex:
        logger.warning(f"Parent communication AI call failed: {ex}. Using grounded fallback.")

    fallback_res = build_grounded_fallback()
    await ai_cache.set(cache_key, fallback_res, ttl_seconds=1800)
    return fallback_res
