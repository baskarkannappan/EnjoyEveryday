from fastapi import APIRouter, Depends, HTTPException
import json
import logging
from typing import List, Dict, Any
from app.schemas.requests import RecommendationRequest
from app.schemas.responses import RecommendationResponse
from app.security.auth import get_tenant_context
from app.security.tenant_context import TenantContext
from app.providers.gemini_provider import gemini_provider
from app.prompts.recommendations import RECOMMENDATIONS_INSTRUCTION
from app.validation.evidence_validator import evidence_validator

logger = logging.getLogger("EnjoyEveryday.Api.Recommendations")
router = APIRouter(tags=["Recommendations"])

@router.post("/api/recommendations", response_model=RecommendationResponse)
async def get_recommendations(
    req: RecommendationRequest,
    context: TenantContext = Depends(get_tenant_context)
):
    if not req.candidateExperiences:
        return {"recommendations": []}

    candidate_dicts = [c.model_dump() for c in req.candidateExperiences[:30]]
    memory_dicts = [m.model_dump() for m in req.evidenceMemories[:30]]
    has_evidence = len(memory_dicts) > 0

    context_payload = {
        "scope": req.scope,
        "targetName": req.targetName,
        "preferences": req.preferences.model_dump() if req.preferences else {},
        "evidenceMemories": memory_dicts,
        "candidateExperiences": candidate_dicts
    }

    prompt = f"Please recommend suitable experiences using this context and evidence:\n{json.dumps(context_payload, indent=2)}"

    try:
        raw_recs = await gemini_provider.generate_json(
            prompt=prompt,
            system_instruction=RECOMMENDATIONS_INSTRUCTION,
            tenant_context=context,
            temperature=0.3
        )
        if isinstance(raw_recs, dict) and "recommendations" in raw_recs:
            raw_recs = raw_recs["recommendations"]
        elif not isinstance(raw_recs, list):
            raw_recs = []
    except Exception as ex:
        logger.warning(f"AI recommendations query failed: {ex}. Using grounded deterministic recommendation fallback.")
        raw_recs = []

    # Post-validation against candidate allowlist
    valid_recs = evidence_validator.validate_candidate_references(
        recommended_items=raw_recs,
        candidate_experiences=candidate_dicts,
        id_field="experienceId",
        title_field="experienceTitle"
    )

    # Post-validation of supporting evidence citations
    for r in valid_recs:
        cites = r.get("supportingEvidence", [])
        r["supportingEvidence"] = evidence_validator.validate_evidence_citations(cites, memory_dicts)
        if not r["supportingEvidence"]:
            r["isEvidenceLimited"] = True

    # If model failed or returned no valid candidates, use deterministic grounded fallback
    if not valid_recs:
        for c in req.candidateExperiences[:3]:
            valid_recs.append({
                "experienceId": c.id,
                "experienceTitle": c.title,
                "suggestedFormat": "Whole Class" if req.scope == "Classroom" else "Individual",
                "estimatedDurationMinutes": req.preferences.preferredDurationMinutes if req.preferences and req.preferences.preferredDurationMinutes else 20,
                "rationale": f"Authoritative curriculum experience from your library suitable for {req.targetName}.",
                "supportingEvidence": [],
                "materials": c.materials,
                "isEvidenceLimited": not has_evidence
            })

    return {"recommendations": valid_recs}
