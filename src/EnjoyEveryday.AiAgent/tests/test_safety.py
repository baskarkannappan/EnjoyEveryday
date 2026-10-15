from app.validation.safety_validator import safety_validator

def test_choking_hazard_flagged_for_under_3():
    experience = {
        "Title": "Sensory Sorting with Marbles",
        "Description": "Toddlers sort small beads and marbles into cups.",
        "TeacherGuidance": "Hand children the small beads.",
        "Materials": ["Small marbles", "Plastic cups"],
        "ExecutionGuide": {
            "Phases": [],
            "Safety": []
        }
    }
    result = safety_validator.validate_experience(experience, target_age_min=2)
    assert not result.is_safe
    assert result.risk_level == "Critical"
    assert any("choking risk" in c.lower() for c in result.flagged_concerns)

def test_sharp_tool_flagged():
    experience = {
        "Title": "Cardboard Box Sculptures",
        "Description": "Cutting boxes with razor blades and box cutters.",
        "TeacherGuidance": "Allow children to cut with box cutter.",
        "Materials": ["Box cutter", "Cardboard"],
        "ExecutionGuide": {
            "Phases": [],
            "Safety": []
        }
    }
    result = safety_validator.validate_experience(experience, target_age_min=4)
    assert not result.is_safe
    assert result.risk_level == "Critical"
    assert any("sharp hazard" in c.lower() for c in result.flagged_concerns)

def test_prohibited_words_detected():
    experience = {
        "Title": "Wobbly Towers",
        "Description": "Build a wiggly shaky tower with wooden blocks.",
        "TeacherGuidance": "Watch the tower wiggle.",
        "Materials": ["Blocks"],
        "ExecutionGuide": {
            "Phases": [],
            "Safety": []
        }
    }
    result = safety_validator.validate_experience(experience, target_age_min=4)
    assert any("prohibited repetitive vocabulary" in c.lower() for c in result.flagged_concerns)

def test_safe_experience_passes():
    experience = {
        "Title": "Nature Leaf Press Collage",
        "Description": "Children collect fallen autumn leaves and press them onto cardstock with washable glue.",
        "TeacherGuidance": "Guide children gently on the outdoor path.",
        "Materials": ["Autumn leaves", "Washable glue stick", "Cardstock"],
        "ExecutionGuide": {
            "Phases": [],
            "Safety": ["Remind children to keep leaves out of mouths"]
        }
    }
    result = safety_validator.validate_experience(experience, target_age_min=4)
    assert result.is_safe
    assert result.risk_level == "Low"
