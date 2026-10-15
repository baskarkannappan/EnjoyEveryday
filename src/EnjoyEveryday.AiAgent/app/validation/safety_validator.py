from dataclasses import dataclass, field
from typing import List, Dict, Any, Optional
import re

@dataclass
class SafetyValidationResult:
    is_safe: bool
    risk_level: str  # "Low", "Medium", "High", "Critical"
    flagged_concerns: List[str] = field(default_factory=list)
    required_supervision_notes: List[str] = field(default_factory=list)
    safer_alternatives: List[str] = field(default_factory=list)

class SafetyValidator:
    # Small parts / choking hazard keywords
    CHOKING_HAZARDS = [
        "small bead", "beads", "marble", "marbles", "button", "buttons",
        "raw bean", "dried beans", "dry beans", "lentil", "lentils", "unpopped popcorn",
        "small coin", "coins", "latex balloon", "balloons", "small screw", "magnets", "small marble"
    ]

    # Sharp tools
    SHARP_HAZARDS = [
        "exacto", "x-acto", "box cutter", "razor", "sharp knife", "glass jar", "broken glass", "stapler"
    ]

    # Heat, chemical, or toxic risks
    HEAT_CHEMICAL_HAZARDS = [
        "hot glue gun", "boiling water", "bleach", "ammonia", "caustic", "super glue", "permanent marker fumes"
    ]

    # Prohibited words per instruction
    PROHIBITED_WORDS = ["wobbly", "wiggly", "wiggle", "shakey", "shaky"]

    @classmethod
    def validate_experience(cls, experience_data: Dict[str, Any], target_age_min: int = 3) -> SafetyValidationResult:
        concerns: List[str] = []
        supervision: List[str] = []
        alternatives: List[str] = []

        # Flatten text to search
        title = experience_data.get("Title", "")
        description = experience_data.get("Description", "")
        guidance = experience_data.get("TeacherGuidance", "")
        materials = " ".join(experience_data.get("Materials", []))
        
        exec_guide = experience_data.get("ExecutionGuide", {})
        safety_notes = " ".join(exec_guide.get("Safety", []))
        phases_text = " ".join([
            f"{p.get('ChildrenDo', '')} {p.get('TeacherDoes', '')}"
            for p in exec_guide.get("Phases", [])
        ])

        combined_text = f"{title} {description} {guidance} {materials} {safety_notes} {phases_text}".lower()

        # 1. Prohibited words check
        for pw in cls.PROHIBITED_WORDS:
            if re.search(rf"\b{pw}\b", combined_text):
                concerns.append(f"Contains prohibited repetitive vocabulary: '{pw}'.")

        # 2. Choking hazards for young children
        for ch in cls.CHOKING_HAZARDS:
            if ch in combined_text:
                if target_age_min < 3:
                    concerns.append(f"High choking risk for children under 3: '{ch}' detected.")
                    alternatives.append(f"Substitute '{ch}' with larger non-ingestible items or dough.")
                else:
                    supervision.append(f"Ensure 1-to-1 or close small-group supervision when handling small loose parts like '{ch}'.")

        # 3. Sharp tools
        for sh in cls.SHARP_HAZARDS:
            if sh in combined_text:
                concerns.append(f"Inappropriate cutting/sharp hazard for daycare: '{sh}' detected.")
                alternatives.append("Use round-tip safety scissors or pre-cut materials by teacher.")

        # 4. Heat / Chemical hazards
        for hh in cls.HEAT_CHEMICAL_HAZARDS:
            if hh in combined_text:
                concerns.append(f"Thermal or chemical hazard detected: '{hh}'.")
                alternatives.append("Use child-safe washable glue sticks or cold water instead.")

        # 5. Water play drowning/slip check
        if "water" in combined_text and ("basin" in combined_text or "tub" in combined_text or "water table" in combined_text):
            if "supervis" not in safety_notes.lower() and "slip" not in safety_notes.lower():
                supervision.append("Active constant teacher supervision required around water play; wipe up splashes immediately to prevent slips.")

        # Determine risk level
        risk_level = "Low"
        if any("High choking risk" in c or "sharp hazard" in c or "Thermal" in c for c in concerns):
            risk_level = "Critical"
        elif len(concerns) > 0 or len(supervision) > 0:
            risk_level = "Medium"

        is_safe = risk_level not in ["Critical"]

        return SafetyValidationResult(
            is_safe=is_safe,
            risk_level=risk_level,
            flagged_concerns=concerns,
            required_supervision_notes=supervision,
            safer_alternatives=alternatives
        )

safety_validator = SafetyValidator()
