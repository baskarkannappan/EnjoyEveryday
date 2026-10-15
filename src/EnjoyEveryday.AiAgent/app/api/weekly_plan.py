from fastapi import APIRouter, Depends, HTTPException
import json
import logging
from typing import List, Dict, Any
from app.schemas.requests import WeeklyPlanRequest
from app.schemas.responses import WeeklyPlanResponse
from app.security.auth import get_tenant_context
from app.security.tenant_context import TenantContext
from app.providers.gemini_provider import gemini_provider
from app.prompts.weekly_plan import WEEKLY_PLAN_INSTRUCTION
from app.validation.evidence_validator import evidence_validator

logger = logging.getLogger("EnjoyEveryday.Api.WeeklyPlan")
router = APIRouter(tags=["Weekly Planning"])

DAYS_ORDER = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]

@router.post("/api/weekly-plan", response_model=WeeklyPlanResponse)
async def get_weekly_plan(
    req: WeeklyPlanRequest,
    context: TenantContext = Depends(get_tenant_context)
):
    if not req.candidateExperiences:
        return {"weekSummary": "No candidate experiences available for planning.", "dailyPlans": []}

    candidate_dicts = [c.model_dump() for c in req.candidateExperiences]
    candidate_map = {str(c["id"]): c for c in candidate_dicts}

    # Filter out recently conducted experiences if enough alternatives exist
    recent_set = set(req.recentExperiences)
    available_candidates = [
        c for c in candidate_dicts
        if c["id"] not in recent_set and c["title"] not in recent_set
    ]
    if len(available_candidates) < 5:
        available_candidates = candidate_dicts

    context_payload = {
        "classroomName": req.classroomName,
        "ageGroup": req.ageGroup or "Pre-K (3-5 years)",
        "weekStartDate": req.weekStartDate,
        "weekEndDate": req.weekEndDate,
        "preferences": req.preferences.model_dump() if req.preferences else {},
        "recentExperiences": req.recentExperiences[:15],
        "candidateExperiences": available_candidates[:30]
    }

    prompt = f"Please generate a balanced Monday-Friday weekly plan draft using these candidate experiences:\n{json.dumps(context_payload, indent=2)}"

    try:
        raw_result = await gemini_provider.generate_json(
            prompt=prompt,
            system_instruction=WEEKLY_PLAN_INSTRUCTION,
            tenant_context=context,
            temperature=0.3
        )
    except Exception as ex:
        logger.warning(f"AI weekly plan generation failed: {ex}. Using deterministic balanced fallback.")
        raw_result = None

    daily_plans = []
    if isinstance(raw_result, dict) and "dailyPlans" in raw_result:
        seen_days = set()
        seen_exp_ids = set()

        for item in raw_result["dailyPlans"]:
            if not isinstance(item, dict):
                continue
            day = item.get("dayOfWeek")
            exp_id = str(item.get("experienceId", ""))

            # Validate day and candidate ID
            if day in DAYS_ORDER and day not in seen_days and exp_id in candidate_map:
                # Anti-duplication check if we have enough distinct candidates
                if len(available_candidates) >= 5 and exp_id in seen_exp_ids:
                    continue

                item["experienceTitle"] = candidate_map[exp_id]["title"]
                seen_days.add(day)
                seen_exp_ids.add(exp_id)
                daily_plans.append(item)

    # Ensure all 5 days exist without gaps
    existing_days = {p.get("dayOfWeek") for p in daily_plans}
    unused_candidates = [c for c in available_candidates if str(c["id"]) not in {p.get("experienceId") for p in daily_plans}]
    cand_idx = 0

    for day in DAYS_ORDER:
        if day not in existing_days:
            if unused_candidates:
                chosen_cand = unused_candidates.pop(0)
            else:
                chosen_cand = available_candidates[cand_idx % len(available_candidates)]
                cand_idx += 1

            materials_str = ", ".join(chosen_cand.get("materials", [])) or "Classroom learning supplies"
            daily_plans.append({
                "dayOfWeek": day,
                "experienceId": chosen_cand["id"],
                "experienceTitle": chosen_cand["title"],
                "learningGoal": f"Hands-on engagement and discovery in {chosen_cand.get('category') or 'creative exploration'}.",
                "groupFormat": "Small Group",
                "estimatedDurationMinutes": 25,
                "timeOfDay": "Morning",
                "requiredMaterials": materials_str,
                "rationale": f"Curriculum experience from active catalog suitable for {req.classroomName}."
            })

    # Sort plans Monday to Friday
    day_indices = {d: i for i, d in enumerate(DAYS_ORDER)}
    daily_plans.sort(key=lambda x: day_indices.get(x.get("dayOfWeek"), 99))

    week_summary = raw_result.get("weekSummary") if isinstance(raw_result, dict) and raw_result.get("weekSummary") else f"Curated weekly plan draft for {req.classroomName} balancing creative play, movement, and hands-on discovery."

    return {
        "weekSummary": week_summary,
        "dailyPlans": daily_plans
    }
