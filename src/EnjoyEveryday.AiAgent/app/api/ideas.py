from fastapi import APIRouter, Depends, HTTPException
import json
import logging
from typing import List, Dict, Any
from app.schemas.requests import IdeaRequest, ImproveRequest, MaterialsRequest
from app.schemas.responses import IdeaResponse, ImproveResponse, MaterialsResponse
from app.security.auth import get_tenant_context
from app.security.tenant_context import TenantContext
from app.providers.gemini_provider import gemini_provider
from app.prompts.ideas import IDEAS_INSTRUCTION
from app.prompts.improve import IMPROVE_INSTRUCTION
from app.prompts.materials import MATERIALS_INSTRUCTION
from app.validation.experience_validator import experience_validator
from app.validation.safety_validator import safety_validator

logger = logging.getLogger("EnjoyEveryday.Api.Ideas")
router = APIRouter(tags=["Experience Design"])

def build_improved_fallback(existing_data: Dict[str, Any], request_context: str) -> Dict[str, Any]:
    base = experience_validator.sanitize_and_validate(existing_data)
    title = base.get("Title") or "Creative Learning Studio"
    desc = base.get("Description") or "Children explore and create together."
    challenge = base.get("Challenge") or "How can we build and explore together?"

    req_clean = request_context.strip()
    title_suffix = f" (Focus: {req_clean.title()})" if req_clean.lower() not in title.lower() else ""

    exec_guide = base.get("ExecutionGuide", {})
    if not exec_guide.get("Phases"):
        exec_guide["Phases"] = [
            {"Name": "Welcome & Exploration", "DurationMinutes": 5, "ChildrenDo": "Gather and inspect materials", "TeacherDoes": f"Introduces the focus on {req_clean}"},
            {"Name": "Active Creation", "DurationMinutes": 15, "ChildrenDo": "Create and collaborate", "TeacherDoes": "Circulates, offering open questions and encouragement"},
            {"Name": "Reflection & Clean-Up", "DurationMinutes": 5, "ChildrenDo": "Tidy materials and share discoveries", "TeacherDoes": "Guides reflection circle"}
        ]
        exec_guide["SuggestedDurationMinutes"] = 25
        exec_guide["Watch"] = "Watch for engaged curiosity and peer sharing."
        exec_guide["Ask"] = f"How did you decide to use {req_clean} in your creation?"
        exec_guide["Try"] = "Try combining two different materials together."

    return experience_validator.sanitize_and_validate({
        "Title": f"{title}{title_suffix}",
        "Description": f"{desc} Adapted with emphasis on: {req_clean}.",
        "Challenge": f"{challenge} Challenge adaptation: {req_clean}.",
        "Together": base.get("Together") or f"Children collaborate in small groups to explore {req_clean}.",
        "Create": base.get("Create") or "A collaborative hands-on project.",
        "ChildChoice": base.get("ChildChoice") or f"Children choose how they incorporate {req_clean} into their play.",
        "MagicMoment": base.get("MagicMoment") or "The excitement when their idea comes to life.",
        "Accomplishment": base.get("Accomplishment") or "Sharing their finished creation with the classroom.",
        "Reflection": base.get("Reflection") or f"What was your favorite part about exploring {req_clean}?",
        "TeacherGuidance": base.get("TeacherGuidance") or f"Set up work trays with accessible supplies. Guide children to explore {req_clean}.",
        "Materials": base.get("Materials") or ["Standard classroom learning materials", "Washable craft supplies"],
        "ExecutionGuide": exec_guide
    })

@router.post("/api/ideas", response_model=IdeaResponse)
async def generate_ideas(
    req: IdeaRequest,
    context: TenantContext = Depends(get_tenant_context)
):
    prompt = f"Learning Theme / Starting Idea: {req.idea.strip()}"
    
    try:
        raw_suggestions = await gemini_provider.generate_json(
            prompt=prompt,
            system_instruction=IDEAS_INSTRUCTION,
            tenant_context=context,
            temperature=0.6
        )
    except Exception as ex:
        logger.warning(f"AI generation failed for /api/ideas: {ex}. Building deterministic safe fallback.")
        raw_suggestions = [
            {
                "Title": f"Exploring {req.idea.title()}: Creative Tactile Studio",
                "Description": f"Children engage in open-ended creative construction and tactile exploration centered on {req.idea}.",
                "Challenge": f"Can we design and build a display inspired by {req.idea}?",
                "Together": "Children share building materials and describe what they are making to their peers.",
                "Create": "A tangible collaborative model or collage.",
                "ChildChoice": "Children choose which textures, shapes, and tools they wish to combine.",
                "MagicMoment": "Noticing how different materials join together to form a new creation.",
                "Accomplishment": "Presenting their completed project during reflection circle.",
                "Reflection": f"What was your favorite texture or tool you used while exploring {req.idea}?",
                "TeacherGuidance": "Introduce materials at the low table. Encourage children to test how pieces fit together.",
                "Materials": ["Cardstock", "Washable craft adhesive", "Wooden craft sticks", "Textured paper scraps"],
                "ExecutionGuide": {
                    "Preparation": ["Cut paper into varied shapes", "Set out trays on low work tables"],
                    "EnvironmentSetup": ["Spacious floor or craft table with accessible trays"],
                    "MaterialSetup": ["Place trays evenly within reach of small groups"],
                    "SuggestedDurationMinutes": 25,
                    "TeacherIntroduction": f"Today we are exploring {req.idea}! Look at all these interesting textures we can build with.",
                    "Phases": [
                        {"Name": "Welcome & Material Touch", "DurationMinutes": 5, "ChildrenDo": "Feel materials and select initial pieces", "TeacherDoes": "Demonstrates gentle handling and asks open questions"},
                        {"Name": "Hands-On Creation", "DurationMinutes": 15, "ChildrenDo": "Assemble and combine elements", "TeacherDoes": "Circulates, providing positive reinforcement"},
                        {"Name": "Sharing & Clean-Up", "DurationMinutes": 5, "ChildrenDo": "Place finished crafts on drying rack and tidy up", "TeacherDoes": "Leads a gentle closing transition"}
                    ],
                    "Watch": "Observe hand-eye coordination and peer turn-taking.",
                    "Ask": "How does that piece feel when you touch it?",
                    "Try": "Try connecting two different shapes together.",
                    "Safety": ["Ensure only child-safe non-toxic adhesive is used", "Remind children that materials remain on the table"],
                    "Adaptations": ["Provide larger chunky pieces for developing fine-motor grip"],
                    "Extensions": ["Take photos of finished structures to create a classroom storybook"],
                    "Closing": "Gather on the carpet for a quick proud celebration of everyone's work.",
                    "LittleMomentOpportunity": "Praise a child who noticed an interesting detail in their friend's project."
                }
            }
        ]

    if not isinstance(raw_suggestions, list):
        if isinstance(raw_suggestions, dict) and "suggestions" in raw_suggestions:
            raw_suggestions = raw_suggestions["suggestions"]
        else:
            raw_suggestions = [raw_suggestions]

    validated_suggestions = []
    for item in raw_suggestions:
        if isinstance(item, dict):
            clean_item = experience_validator.sanitize_and_validate(item)
            safety_res = safety_validator.validate_experience(clean_item)
            if safety_res.required_supervision_notes:
                current_safety = clean_item["ExecutionGuide"].get("Safety", [])
                for note in safety_res.required_supervision_notes:
                    if note not in current_safety:
                        current_safety.append(note)
                clean_item["ExecutionGuide"]["Safety"] = current_safety

            validated_suggestions.append(clean_item)

    return {"suggestions": validated_suggestions}

@router.post("/api/improve", response_model=ImproveResponse)
async def improve_experience(
    req: ImproveRequest,
    context: TenantContext = Depends(get_tenant_context)
):
    prompt = f"Improvement Request: {req.requestContext.strip()}\n\nCurrent Experience DNA:\n{json.dumps(req.experienceDna, indent=2)}"
    
    try:
        raw_result = await gemini_provider.generate_json(
            prompt=prompt,
            system_instruction=IMPROVE_INSTRUCTION,
            tenant_context=context,
            temperature=0.4
        )
        reason = raw_result.get("reason", "Experience enhanced with requested pedagogical adjustments.")
        updated_data = raw_result.get("updatedData", req.experienceDna)
        clean_data = experience_validator.sanitize_and_validate(updated_data)

        # Merge with fallback if any critical fields were missed by model
        if not clean_data.get("Title") or not clean_data.get("Description"):
            fallback = build_improved_fallback(req.experienceDna, req.requestContext)
            for k, v in fallback.items():
                if not clean_data.get(k):
                    clean_data[k] = v

        return {"reason": reason, "updatedData": clean_data}

    except Exception as ex:
        logger.warning(f"AI improvement failed: {ex}. Returning safe grounded fallback.")
        fallback = build_improved_fallback(req.experienceDna, req.requestContext)
        return {
            "reason": f"Adapted experience structure to incorporate '{req.requestContext}'.",
            "updatedData": fallback
        }

@router.post("/api/materials", response_model=MaterialsResponse)
async def extract_materials(
    req: MaterialsRequest,
    context: TenantContext = Depends(get_tenant_context)
):
    existing_mats = req.experienceDna.get("Materials", []) or req.experienceDna.get("materials", [])
    deduped = set()
    for m in existing_mats:
        if isinstance(m, str) and m.strip():
            deduped.add(m.strip().title())

    prompt = f"Experience Design:\n{json.dumps(req.experienceDna, indent=2)}"
    try:
        raw_mats = await gemini_provider.generate_json(
            prompt=prompt,
            system_instruction=MATERIALS_INSTRUCTION,
            tenant_context=context,
            temperature=0.2
        )
        if isinstance(raw_mats, list):
            for m in raw_mats:
                if isinstance(m, str) and m.strip():
                    deduped.add(m.strip().title())
    except Exception as ex:
        logger.warning(f"Model material deduction skipped: {ex}. Using deterministic extracted items.")

    final_list = sorted(list(deduped))
    if not final_list:
        final_list = ["Standard classroom learning materials"]

    return {"materials": final_list}
