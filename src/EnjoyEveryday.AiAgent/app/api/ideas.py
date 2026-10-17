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
    
    # Strip existing awkward artifacts if present
    for artifact in ["(Focus:", "Focus:", "Adapted with emphasis on:"]:
        if artifact.lower() in title.lower():
            title = title.split("(")[0].strip()
    if title.lower().startswith("exploring "):
        core_topic = title[10:].strip()
        if ":" in core_topic:
            core_topic = core_topic.split(":")[0].strip()
    else:
        core_topic = title.split(":")[0].strip()

    req_clean = request_context.strip()
    is_playful = "playful" in req_clean.lower()
    is_choice = "choice" in req_clean.lower()
    is_sensory = "sensory" in req_clean.lower()
    is_outdoor = "outdoor" in req_clean.lower()

    if is_playful:
        new_title = f"The Playful World of {core_topic.title()}"
        new_desc = f"Children embark on a whimsical, playful adventure with {core_topic.lower()}, turning tactile curiosity into imaginative storytelling, roleplay, and joyful collaboration."
        new_challenge = f"Can we build a miniature play village or friendly creature using our {core_topic.lower()}?"
        new_choice = "Children invent their own characters, choose their favorite textures and shapes, and guide the story."
        new_together = "Children connect their individual creations together to form a lively shared fantasy world."
        new_magic = "The sheer delight when their creations interact and tell a story together."
    elif is_choice:
        new_title = f"{core_topic.title()}: Child-Led Creative Studio"
        new_desc = f"An open-ended, child-guided exploration where little learners choose their tools, decide their own pace, and experiment freely with {core_topic.lower()}."
        new_challenge = f"What amazing discoveries can you make today? You decide what to create and how to test it!"
        new_choice = "Children freely select from various tools, molds, colors, and textures, deciding whether to work solo or with peers."
        new_together = "Children observe each other's ideas, trade tools, and offer helpful suggestions."
        new_magic = "A child's independent realization that they solved a challenge using their own unique method."
    elif is_sensory:
        new_title = f"{core_topic.title()}: Sensory Touch & Texture Lab"
        new_desc = f"A rich tactile adventure inviting children to squeeze, roll, imprint, and observe physical transformations through immersive sensory play."
        new_challenge = f"How many different patterns, textures, and sensations can we press into our materials?"
        new_choice = "Children choose between textured rollers, natural stamps, stamps, and sensory tools."
        new_together = "Children share textured impressions and compare how different surfaces feel."
        new_magic = "Discovering an intricate pattern revealed under the magnifying glass or light table."
    elif is_outdoor:
        new_title = f"{core_topic.title()}: Outdoor Nature Studio"
        new_desc = f"Taking creative exploration into the fresh air, blending natural elements with {core_topic.lower()} in sunlight and open space."
        new_challenge = f"Can we combine outdoor treasures (leaves, stones, twigs) with our materials to build a nature garden?"
        new_choice = "Children explore the outdoor perimeter to collect their own organic crafting accents."
        new_together = "Small groups work together on the outdoor work table or lawn."
        new_magic = "Seeing how natural sunlight and botanical textures transform their finished work."
        base["Environment"] = "Outdoor"
    else:
        new_title = f"{core_topic.title()} Adventure Studio"
        new_desc = f"Children engage in dynamic, hands-on exploration centered on {core_topic.lower()}, tailored to foster {req_clean.lower()}."
        new_challenge = f"How can we explore {core_topic.lower()} in a brand new way today?"
        new_choice = f"Children choose how they incorporate {req_clean.lower()} into their creations."
        new_together = f"Children share materials and collaborate in small groups."
        new_magic = "A wonderful breakthrough when their collaborative idea takes shape."

    exec_guide = base.get("ExecutionGuide", {})
    if not exec_guide.get("Phases"):
        exec_guide["Phases"] = [
            {"Name": "Welcome & Sensory Invitation", "DurationMinutes": 5, "ChildrenDo": f"Touch and explore {core_topic.lower()} with open curiosity", "TeacherDoes": f"Introduces the playful theme with an inviting question"},
            {"Name": "Hands-On Play & Creation", "DurationMinutes": 15, "ChildrenDo": "Shape, build, and invent together in small groups", "TeacherDoes": "Circulates, offering encouragement, playful prompts, and listening closely"},
            {"Name": "Showcase & Circle Sharing", "DurationMinutes": 5, "ChildrenDo": "Proudly display creations and describe what they made", "TeacherDoes": "Leads a gentle reflection and celebration of each child's creativity"}
        ]
        exec_guide["SuggestedDurationMinutes"] = 25
        exec_guide["Watch"] = "Watch for moments of joyful collaboration, fine-motor experimentation, and peer sharing."
        exec_guide["Ask"] = f"What does your creation remind you of? Tell me about what's happening here!"
        exec_guide["Try"] = "Try adding a new texture or combining two pieces to see what happens."

    return experience_validator.sanitize_and_validate({
        "Title": new_title,
        "Description": new_desc,
        "Challenge": new_challenge,
        "Together": new_together,
        "Create": base.get("Create") or f"A collaborative hands-on project exploring {core_topic.lower()}.",
        "ChildChoice": new_choice,
        "MagicMoment": new_magic,
        "Accomplishment": base.get("Accomplishment") or "Sharing their finished creation with pride during group circle time.",
        "Reflection": base.get("Reflection") or f"What was your favorite part of playing with {core_topic.lower()} today?",
        "TeacherGuidance": base.get("TeacherGuidance") or f"Set up materials at child height. Encourage open exploration without rigid right-or-wrong rules.",
        "AgeMin": base.get("AgeMin", 3),
        "AgeMax": base.get("AgeMax", 5),
        "GroupSizeMin": base.get("GroupSizeMin", 2),
        "GroupSizeMax": base.get("GroupSizeMax", 6),
        "Environment": base.get("Environment", "Indoor"),
        "DevelopmentalGoals": base.get("DevelopmentalGoals", ["Fine Motor Skills", "Creative Expression", "Social Cooperation"]),
        "Materials": base.get("Materials") or [core_topic.title(), "Natural texture tools", "Child-safe work trays"],
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
