import pytest
from fastapi.testclient import TestClient
from app.main import app
from app.providers.gemini_provider import gemini_provider

client = TestClient(app)
AUTH_HEADERS = {"X-Tenant-Api-Key": "test_valid_api_key_1234567890"}

@pytest.fixture(autouse=True)
def reset_mock_provider():
    gemini_provider.set_mock_handler(None)
    yield
    gemini_provider.set_mock_handler(None)

def test_api_ideas_fallback():
    # Calling ideas endpoint without external network triggers safe deterministic fallback
    response = client.post("/api/ideas", headers=AUTH_HEADERS, json={"idea": "Autumn Leaf Painting"})
    assert response.status_code == 200
    data = response.json()
    assert "suggestions" in data
    assert len(data["suggestions"]) >= 1
    sug = data["suggestions"][0]
    assert "Title" in sug
    assert "ExecutionGuide" in sug
    assert isinstance(sug["ExecutionGuide"]["Watch"], str)
    assert isinstance(sug["ExecutionGuide"]["Ask"], str)

def test_api_improve():
    req = {
        "experienceDna": {
            "Title": "Block Castle",
            "Description": "Building castles with blocks",
            "Materials": ["Wooden blocks"]
        },
        "requestContext": "Make it more sensory and collaborative"
    }
    response = client.post("/api/improve", headers=AUTH_HEADERS, json=req)
    assert response.status_code == 200
    data = response.json()
    assert "reason" in data
    assert "updatedData" in data

def test_api_materials():
    req = {
        "experienceDna": {
            "Title": "Sensory Finger Paint",
            "Materials": ["Finger paint", "Cardstock paper"]
        }
    }
    response = client.post("/api/materials", headers=AUTH_HEADERS, json=req)
    assert response.status_code == 200
    data = response.json()
    assert "materials" in data
    assert "Finger Paint" in data["materials"]
    assert "Cardstock Paper" in data["materials"]

def test_api_recommendations_candidate_allowlist():
    candidate_id = "11111111-1111-1111-1111-111111111111"
    req = {
        "scope": "Classroom",
        "targetName": "Sunshine Room",
        "candidateExperiences": [
            {
                "id": candidate_id,
                "title": "Clay Animal Sculptures",
                "category": "Creative Arts",
                "materials": ["Modeling clay"]
            }
        ],
        "evidenceMemories": []
    }
    response = client.post("/api/recommendations", headers=AUTH_HEADERS, json=req)
    assert response.status_code == 200
    data = response.json()
    assert "recommendations" in data
    assert len(data["recommendations"]) == 1
    rec = data["recommendations"][0]
    assert rec["experienceId"] == candidate_id
    assert rec["experienceTitle"] == "Clay Animal Sculptures"
    assert rec["isEvidenceLimited"] is True

def test_api_journey_insights_empty_evidence():
    req = {
        "childId": "22222222-2222-2222-2222-222222222222",
        "childName": "Maya",
        "periodType": "Last30Days",
        "periodStartDate": "2026-09-01",
        "periodEndDate": "2026-09-30",
        "evidenceMemories": [],
        "candidateExperiences": [
            {
                "id": "33333333-3333-3333-3333-333333333333",
                "title": "Water Table Exploration",
                "category": "Sensory"
            }
        ]
    }
    response = client.post("/api/journey-insights", headers=AUTH_HEADERS, json=req)
    assert response.status_code == 200
    data = response.json()
    assert "overallSummary" in data
    assert "items" in data
    # When evidence is empty, journey summary must report that neutrally without fabricating activities
    assert any("No active observations" in item.get("statement", "") for item in data["items"])

def test_api_weekly_plan_five_days():
    candidates = [
        {"id": f"00000000-0000-0000-0000-00000000000{i}", "title": f"Activity {i}", "category": "Play"}
        for i in range(1, 6)
    ]
    req = {
        "classroomId": "44444444-4444-4444-4444-444444444444",
        "classroomName": "Toddler Explorers",
        "weekStartDate": "2026-10-19",
        "weekEndDate": "2026-10-23",
        "candidateExperiences": candidates
    }
    response = client.post("/api/weekly-plan", headers=AUTH_HEADERS, json=req)
    assert response.status_code == 200
    data = response.json()
    assert "dailyPlans" in data
    assert len(data["dailyPlans"]) == 5
    days = [p["dayOfWeek"] for p in data["dailyPlans"]]
    assert days == ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
    # Verify no duplicate candidate IDs since we have 5 distinct candidates
    exp_ids = [p["experienceId"] for p in data["dailyPlans"]]
    assert len(set(exp_ids)) == 5

def test_api_parent_communication():
    req = {
        "childName": "Leo",
        "classroomName": "Butterfly Room",
        "recipientName": "Sarah",
        "completedExperiences": ["Sand Castle Architecture"],
        "approvedMoments": ["Shared the shovel with a peer"],
        "careHighlights": ["Ate all of his lunch and rested well"]
    }
    response = client.post("/api/parent-communication", headers=AUTH_HEADERS, json=req)
    assert response.status_code == 200
    data = response.json()
    assert "title" in data
    assert "greeting" in data
    assert "Dear Sarah" in data["greeting"]
    assert "highlights" in data
    assert "familyConversationStarter" in data

def test_api_organization_intelligence():
    req = {
        "scopeName": "Little Stars Daycare",
        "reportingPeriod": "Last7Days",
        "attendanceRate": 94.5,
        "completedExperiences": 14,
        "upcomingExperiences": 18,
        "uniqueExperiences": 8,
        "varietyDiversityRate": 85.0,
        "classroomsNeedingPlansCount": 1,
        "classroomsNeedingPlans": ["Infant Nest"]
    }
    response = client.post("/api/organization-intelligence", headers=AUTH_HEADERS, json=req)
    assert response.status_code == 200
    insights = response.json()
    assert isinstance(insights, list)
    assert len(insights) >= 4
    categories = [item["category"] for item in insights]
    assert "OperationalOverview" in categories
    assert "CurriculumVariety" in categories
    assert "PlanningGaps" in categories
