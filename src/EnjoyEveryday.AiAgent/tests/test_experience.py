from app.validation.experience_validator import experience_validator

def test_watch_ask_try_enforced_as_strings():
    # If LLM produces array for Watch, Ask, Try, validator must convert to single string for .NET compatibility
    raw = {
        "Title": "Building Bridges",
        "ExecutionGuide": {
            "Watch": ["Look for balance", "Look for peer collaboration"],
            "Ask": ["How tall can it go?", "What holds it up?"],
            "Try": ["Try adding another block"],
            "SuggestedDurationMinutes": 20,
            "Phases": [
                {"Name": "Intro", "DurationMinutes": 5},
                {"Name": "Build", "DurationMinutes": 15}
            ]
        }
    }
    cleaned = experience_validator.sanitize_and_validate(raw)
    exec_guide = cleaned["ExecutionGuide"]
    assert isinstance(exec_guide["Watch"], str)
    assert "Look for balance Look for peer collaboration" == exec_guide["Watch"]
    assert isinstance(exec_guide["Ask"], str)
    assert isinstance(exec_guide["Try"], str)

def test_phase_duration_reconciliation():
    raw = {
        "Title": "Sensory Cloud Dough",
        "ExecutionGuide": {
            "SuggestedDurationMinutes": 10, # Contradicts phase sum (25)
            "Phases": [
                {"Name": "Mix", "DurationMinutes": 10},
                {"Name": "Knead", "DurationMinutes": 10},
                {"Name": "Play", "DurationMinutes": 5}
            ]
        }
    }
    cleaned = experience_validator.sanitize_and_validate(raw)
    assert cleaned["ExecutionGuide"]["SuggestedDurationMinutes"] == 25

def test_missing_root_fields_filled():
    raw = {
        "Title": "Water Wonders"
    }
    cleaned = experience_validator.sanitize_and_validate(raw)
    for f in experience_validator.REQUIRED_ROOT_FIELDS:
        assert f in cleaned
        assert cleaned[f] is not None
