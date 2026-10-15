from typing import Dict, Any, List

class ExperienceValidator:
    REQUIRED_ROOT_FIELDS = [
        "Title", "Description", "Challenge", "Together", "Create",
        "ChildChoice", "MagicMoment", "Accomplishment", "Reflection",
        "TeacherGuidance", "Materials", "ExecutionGuide"
    ]

    REQUIRED_EXEC_FIELDS = [
        "Preparation", "EnvironmentSetup", "MaterialSetup",
        "SuggestedDurationMinutes", "TeacherIntroduction", "Phases",
        "Watch", "Ask", "Try", "Safety", "Adaptations", "Extensions",
        "Closing", "LittleMomentOpportunity"
    ]

    @classmethod
    def sanitize_and_validate(cls, data: Dict[str, Any]) -> Dict[str, Any]:
        """Ensures compliance with Experience DNA contract expected by .NET and sanitizes formatting."""
        if not isinstance(data, dict):
            raise ValueError("Experience data must be a dictionary.")

        def get_ci(d: Dict[str, Any], key: str, default=None):
            if not isinstance(d, dict):
                return default
            for k, v in d.items():
                if k.lower() == key.lower():
                    return v
            return default

        cleaned: Dict[str, Any] = {}

        # 1. Map all root fields case-insensitively to exact PascalCase expected by .NET
        for f in cls.REQUIRED_ROOT_FIELDS:
            val = get_ci(data, f)
            if f == "Materials":
                cleaned[f] = val if isinstance(val, list) else []
            elif f == "ExecutionGuide":
                cleaned[f] = val if isinstance(val, dict) else {}
            else:
                cleaned[f] = str(val) if val is not None else ""

        # 2. Execution guide normalization
        raw_exec = cleaned.get("ExecutionGuide", {})
        if not isinstance(raw_exec, dict):
            raw_exec = {}

        exec_guide: Dict[str, Any] = {}
        for ef in cls.REQUIRED_EXEC_FIELDS:
            val = get_ci(raw_exec, ef)
            if ef in ["Preparation", "EnvironmentSetup", "MaterialSetup", "Safety", "Adaptations", "Extensions"]:
                exec_guide[ef] = val if isinstance(val, list) else []
            elif ef == "Phases":
                exec_guide[ef] = val if isinstance(val, list) else []
            elif ef in ["Watch", "Ask", "Try"]:
                if isinstance(val, list):
                    exec_guide[ef] = " ".join(str(item) for item in val)
                elif val is None:
                    exec_guide[ef] = ""
                else:
                    exec_guide[ef] = str(val)
            elif ef == "SuggestedDurationMinutes":
                try:
                    dur_val = int(val) if val is not None else 25
                except (ValueError, TypeError):
                    dur_val = 25
                exec_guide[ef] = max(5, dur_val)
            else:
                exec_guide[ef] = str(val) if val is not None else ""

        # 4. Phase duration reconciliation and case-insensitive phase mapping
        phases = exec_guide.get("Phases", [])
        if isinstance(phases, list):
            valid_phases = []
            phase_sum = 0
            for idx, p in enumerate(phases):
                if isinstance(p, dict):
                    dur = int(get_ci(p, "DurationMinutes", 5) or 5)
                    if dur <= 0:
                        dur = 5
                    phase_sum += dur
                    valid_phases.append({
                        "Name": str(get_ci(p, "Name", f"Phase {idx+1}")),
                        "DurationMinutes": dur,
                        "ChildrenDo": str(get_ci(p, "ChildrenDo", "")),
                        "TeacherDoes": str(get_ci(p, "TeacherDoes", ""))
                    })
            exec_guide["Phases"] = valid_phases

            suggested_dur = int(exec_guide.get("SuggestedDurationMinutes", 0) or 0)
            if suggested_dur <= 0 or (phase_sum > 0 and abs(suggested_dur - phase_sum) > 10):
                exec_guide["SuggestedDurationMinutes"] = max(15, phase_sum)

        cleaned["ExecutionGuide"] = exec_guide

        # 5. Clean materials list
        mats = cleaned.get("Materials", [])
        if isinstance(mats, list):
            seen = set()
            clean_mats = []
            for m in mats:
                s = str(m).strip()
                if s and s.lower() not in seen:
                    seen.add(s.lower())
                    clean_mats.append(s)
            cleaned["Materials"] = clean_mats
        else:
            cleaned["Materials"] = []

        return cleaned

experience_validator = ExperienceValidator()
