from fastapi import APIRouter, Depends, HTTPException
import json
import logging
from typing import List, Dict, Any
from app.schemas.requests import JourneyInsightRequest
from app.schemas.responses import JourneyInsightResponse
from app.security.auth import get_tenant_context
from app.security.tenant_context import TenantContext
from app.providers.provider_registry import ai_provider_registry
from app.prompts.journey_insights import JOURNEY_INSIGHTS_INSTRUCTION
from app.validation.evidence_validator import evidence_validator
from app.cache.ai_cache import ai_cache

logger = logging.getLogger("EnjoyEveryday.Api.JourneyInsights")
router = APIRouter(tags=["Child Journey Insights"])

@router.post("/api/journey-insights", response_model=JourneyInsightResponse)
async def get_journey_insights(
    req: JourneyInsightRequest,
    context: TenantContext = Depends(get_tenant_context)
):
    cache_key = ai_cache.generate_key(context.tenant_id, "journey_insights", req.model_dump())
    cached_insights = await ai_cache.get(cache_key)
    if cached_insights is not None:
        return cached_insights

    evidence_count = len(req.evidenceMemories)
    memory_dicts = [m.model_dump() for m in req.evidenceMemories[:40]]
    candidate_dicts = [c.model_dump() for c in req.candidateExperiences[:20]]

    context_payload = {
        "childName": req.childName,
        "periodType": req.periodType,
        "periodStartDate": req.periodStartDate,
        "periodEndDate": req.periodEndDate,
        "evidenceCount": evidence_count,
        "evidenceMemories": memory_dicts,
        "candidateExperiences": candidate_dicts
    }

    compact_payload = json.dumps(context_payload, separators=(',', ':'))
    prompt = f"Please generate evidence-grounded child journey insights for {req.childName} based on these records:\n{compact_payload}"
    provider = ai_provider_registry.get_provider()

    try:
        raw_result = await provider.generate_json(
            prompt=prompt,
            system_instruction=JOURNEY_INSIGHTS_INSTRUCTION,
            tenant_context=context,
            temperature=0.3
        )
    except Exception as ex:
        logger.warning(f"AI journey insights failed: {ex}. Using grounded deterministic fallback.")
        raw_result = None

    if not isinstance(raw_result, dict) or "items" not in raw_result:
        fallback_items = []
        rank = 1

        if evidence_count > 0:
            top_mem = memory_dicts[0]
            fallback_items.append({
                "category": "JourneySummary",
                "classification": "RecordedFact",
                "statement": f"{req.childName} participated in {evidence_count} recorded learning moments between {req.periodStartDate} and {req.periodEndDate}.",
                "evidenceStrength": "Supported",
                "supportingEvidence": [{
                    "sourceId": top_mem.get("sourceId"),
                    "sourceType": top_mem.get("sourceType"),
                    "title": top_mem.get("title"),
                    "summary": top_mem.get("description") or top_mem.get("title")
                }],
                "suggestedExperienceId": None,
                "suggestedExperienceTitle": None,
                "rankOrder": rank
            })
            rank += 1
        else:
            fallback_items.append({
                "category": "JourneySummary",
                "classification": "RecordedFact",
                "statement": f"No active observations or experience records were logged for {req.childName} between {req.periodStartDate} and {req.periodEndDate}.",
                "evidenceStrength": "Limited",
                "supportingEvidence": [],
                "suggestedExperienceId": None,
                "suggestedExperienceTitle": None,
                "rankOrder": rank
            })
            rank += 1

        if candidate_dicts:
            cand = candidate_dicts[0]
            fallback_items.append({
                "category": "IdeasToConsider",
                "classification": "EducationalSuggestion",
                "statement": f"Consider exploring '{cand.get('title')}' to provide structured opportunities for creative and hands-on discovery.",
                "evidenceStrength": "Emerging",
                "supportingEvidence": [],
                "suggestedExperienceId": cand.get("id"),
                "suggestedExperienceTitle": cand.get("title"),
                "rankOrder": rank
            })

        fallback_res = {
            "overallSummary": f"Records reflect {evidence_count} captured moments for {req.childName} during the requested period.",
            "evidenceGaps": "Records are limited for this period; ongoing observations will deepen future insights." if evidence_count < 3 else None,
            "items": fallback_items
        }
        await ai_cache.set(cache_key, fallback_res, ttl_seconds=1800)
        return fallback_res

    # Validate output items
    candidate_map = {str(c["id"]): c["title"] for c in candidate_dicts if "id" in c}
    items = raw_result.get("items", [])
    valid_items = []

    for item in items:
        if not isinstance(item, dict):
            continue

        s_id = item.get("suggestedExperienceId")
        if s_id and str(s_id) not in candidate_map:
            item["suggestedExperienceId"] = None
            item["suggestedExperienceTitle"] = None
        elif s_id and str(s_id) in candidate_map:
            item["suggestedExperienceTitle"] = candidate_map[str(s_id)]

        cites = item.get("supportingEvidence", [])
        item["supportingEvidence"] = evidence_validator.validate_evidence_citations(cites, memory_dicts)
        valid_items.append(item)

    result = {
        "overallSummary": raw_result.get("overallSummary", f"Progress review for {req.childName}."),
        "evidenceGaps": raw_result.get("evidenceGaps"),
        "items": valid_items
    }
    await ai_cache.set(cache_key, result, ttl_seconds=1800)
    return result
