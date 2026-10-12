from fastapi import FastAPI, Header, HTTPException
from pydantic import BaseModel
import json
import uvicorn
import google.generativeai as genai

app = FastAPI(title="EnjoyEveryday AI Agent")

IDEAS_INSTRUCTION = """You are an expert early childhood education teacher.
The user will give you an idea for an experience for children (ages 3-5).
Suggest 3 possible directions for this idea.
Each direction must be a fully formed Experience idea, providing a structured response.
IMPORTANT: Make the instructions to the teacher very simple. Clearly list all required materials and provide direct, easy-to-follow, step-by-step instructions on exactly how to use them. Avoid overly complex setups or lengthy paragraphs.
IMPORTANT: Do NOT use the words "wobbly", "wiggly", "wiggle", "shakey", or "shaky". Use a more diverse and professional vocabulary, avoiding repetitive childish adjectives.
Return the result EXACTLY as a JSON array of objects.
Each object must match this structure:
{
  "Title": "...",
  "Description": "...",
  "Challenge": "...",
  "Together": "...",
  "Create": "...",
  "ChildChoice": "...",
  "MagicMoment": "...",
  "Accomplishment": "...",
  "Reflection": "...",
  "TeacherGuidance": "...",
  "Materials": ["..."],
  "ExecutionGuide": {
    "Preparation": ["..."],
    "EnvironmentSetup": ["..."],
    "MaterialSetup": ["..."],
    "SuggestedDurationMinutes": 25,
    "TeacherIntroduction": "...",
    "Phases": [
      { "Name": "...", "DurationMinutes": 5, "ChildrenDo": "...", "TeacherDoes": "..." }
    ],
    "Watch": "...",
    "Ask": "...",
    "Try": "...",
    "Safety": ["..."],
    "Adaptations": ["..."],
    "Extensions": ["..."],
    "Closing": "...",
    "LittleMomentOpportunity": "..."
  }
}
Do NOT use markdown code blocks (```json) in your response. Just output the raw array.
IMPORTANT: "Watch", "Ask", and "Try" must be single strings, NOT arrays. Combine multiple points into one string if needed.
"""

IMPROVE_INSTRUCTION = """You are an expert early childhood education teacher.
The user will give you a JSON representation of an existing Experience design and a specific improvement request like 'Make it more playful' or 'Add more child choice'.
Your job is to rewrite the ENTIRE experience in a single shot to heavily incorporate the request. You must update ALL sections to make them cohesive, engaging, and directly address the user's request.
IMPORTANT: Make the instructions to the teacher very simple. Clearly list all required materials in 'Materials' and provide direct, easy-to-follow, step-by-step instructions on exactly how to organize and use them in 'TeacherGuidance' and 'ExecutionGuide'. Avoid overly complex setups or lengthy paragraphs.
IMPORTANT: Do NOT use the words "wobbly", "wiggly", "wiggle", "shakey", or "shaky". Use a more diverse and professional vocabulary, avoiding repetitive childish adjectives.

Return the result EXACTLY as a JSON object matching this structure:
{
  "reason": "Why this completely improves the experience based on the request.",
  "updatedData": {
    "Title": "New or improved title",
    "Description": "New or improved description",
    "Challenge": "...",
    "Together": "...",
    "Create": "...",
    "ChildChoice": "...",
    "MagicMoment": "...",
    "Accomplishment": "...",
    "Reflection": "...",
    "TeacherGuidance": "Detailed instructions on how the teacher should organize and facilitate this...",
    "Materials": ["Item 1", "Item 2", "Item 3"],
    "ExecutionGuide": {
      "Preparation": ["step 1", "step 2"],
      "EnvironmentSetup": ["step 1"],
      "MaterialSetup": ["step 1"],
      "SuggestedDurationMinutes": 25,
      "TeacherIntroduction": "short intro to say",
      "Phases": [
        { "Name": "Phase 1", "DurationMinutes": 5, "ChildrenDo": "action", "TeacherDoes": "action" }
      ],
      "Watch": "...",
      "Ask": "...",
      "Try": "...",
      "Safety": ["..."],
      "Adaptations": ["..."],
      "Extensions": ["..."],
      "Closing": "...",
      "LittleMomentOpportunity": "..."
    }
  }
}
Do NOT use markdown code blocks (```json) in your response. Just output the raw JSON object.
IMPORTANT: "Watch", "Ask", and "Try" must be single strings, NOT arrays. Combine multiple points into one string if needed.
"""

MATERIALS_INSTRUCTION = """You are an expert early childhood education teacher.
The user will give you a JSON representation of an existing Experience design.
Your job is to read the experience design, especially the 'TeacherGuidance', 'ExecutionGuide', and the DNA sections, and deduce all physical materials that are explicitly mentioned or implicitly required to run this experience successfully.
Return the result EXACTLY as a JSON array of strings. Do NOT return an object, just the array of strings.
Example: ["Watercolor paint", "Brushes", "Paper", "Cups for water", "Smocks"]
"""

class IdeaRequest(BaseModel):
    idea: str

class IdeaResponse(BaseModel):
    suggestions: list[dict]

@app.post("/api/ideas", response_model=IdeaResponse)
async def generate_ideas(req: IdeaRequest, x_tenant_api_key: str | None = Header(default=None)):
    if not x_tenant_api_key:
        raise HTTPException(status_code=401, detail="X-Tenant-Api-Key header is missing. Please configure your AI API key in Settings.")

    try:
        genai.configure(api_key=x_tenant_api_key)
        model = genai.GenerativeModel("gemini-3.5-flash", system_instruction=IDEAS_INSTRUCTION)
        response = model.generate_content(req.idea)
        text = response.text
        
        if text.startswith("```json"):
            text = text[7:]
        if text.startswith("```"):
            text = text[3:]
        if text.endswith("```"):
            text = text[:-3]
            
        suggestions = json.loads(text.strip())
        return {"suggestions": suggestions}
    except Exception as e:
        print(f"Error generating ideas: {e}")
        return {"suggestions": []}

class ImproveRequest(BaseModel):
    experienceDna: dict
    requestContext: str

@app.post("/api/improve")
async def improve_experience(req: ImproveRequest, x_tenant_api_key: str | None = Header(default=None)):
    if not x_tenant_api_key:
        raise HTTPException(status_code=401, detail="X-Tenant-Api-Key header is missing. Please configure your AI API key in Settings.")

    try:
        genai.configure(api_key=x_tenant_api_key)
        model = genai.GenerativeModel("gemini-3.5-flash", system_instruction=IMPROVE_INSTRUCTION)
        
        prompt = f"Improvement Request: {req.requestContext}\n\nCurrent Experience DNA:\n{json.dumps(req.experienceDna, indent=2)}"
        response = model.generate_content(prompt)
        text = response.text
        
        if text.startswith("```json"):
            text = text[7:]
        if text.startswith("```"):
            text = text[3:]
        if text.endswith("```"):
            text = text[:-3]
            
        result = json.loads(text.strip())
        return result
    except Exception as e:
        print(f"Error improving experience: {e}")
        return {
          "reason": "Fallback suggestion due to error.",
          "updatedData": {
             "Title": "Error Title",
             "Description": "Error Description",
             "Challenge": "...",
             "Together": "...",
             "Create": "...",
             "ChildChoice": "...",
             "MagicMoment": "...",
             "Accomplishment": "...",
             "Reflection": "...",
             "TeacherGuidance": "...",
             "Materials": [],
             "ExecutionGuide": {
                "Preparation": [],
                "EnvironmentSetup": [],
                "MaterialSetup": [],
                "SuggestedDurationMinutes": 0,
                "TeacherIntroduction": "",
                "Phases": [],
                "Watch": "",
                "Ask": "",
                "Try": "",
                "Safety": [],
                "Adaptations": [],
                "Extensions": [],
                "Closing": "",
                "LittleMomentOpportunity": ""
             }
          }
        }

RECOMMENDATIONS_INSTRUCTION = """You are an expert early childhood education planning advisor in the EnjoyEveryday SaaS system.
Your job is to recommend 3 to 5 learning experiences from the provided catalog of candidate experiences for a child or classroom.

STRICT GROUNDING & SAFETY RULES:
1. Grounding in Actual Records: You must ONLY recommend experiences that exist in the provided 'candidateExperiences' list. Never invent experience titles, descriptions, or IDs.
2. Grounding in Evidence: When explaining why an activity is suitable, you must ONLY cite facts, observations, and interests that are explicitly documented in the provided 'evidenceMemories'.
3. Do NOT Hallucinate: Do NOT invent observations, child moods, developmental milestones, historical events, diagnoses, or parental feedback.
4. Limited Evidence Handling: If 'evidenceMemories' is empty or has minimal information, state clearly that evidence is limited, mark 'isEvidenceLimited' as true, and recommend suitable general classroom experiences based on age and category.
5. Supporting Evidence References: In 'supportingEvidence', list the specific 'sourceId', 'sourceType', and 'title' from the evidence memories that justify the suggestion.
6. Return format: Return the result EXACTLY as a JSON array of objects. Do NOT use markdown code blocks (```json).

Each object in the array must match this schema:
{
  "experienceId": "UUID string matching candidate experience",
  "experienceTitle": "Title matching candidate experience",
  "suggestedFormat": "Individual" or "Small Group" or "Whole Class",
  "estimatedDurationMinutes": 20,
  "rationale": "Clear, reassuring explanation grounded in actual evidence.",
  "supportingEvidence": [
    {
      "sourceId": "UUID string from evidence",
      "sourceType": "child_journey_entries" or "child_stories" or "experience_participations" or "experience_feedbacks",
      "summary": "Brief reference phrase, e.g., 'Observed interest in building blocks on Oct 8'"
    }
  ],
  "materials": ["Material 1", "Material 2"],
  "isEvidenceLimited": false
}
"""

class RecommendationCandidate(BaseModel):
    id: str
    title: str
    description: str | None = None
    category: str | None = None
    ageRange: str | None = None
    materials: list[str] = []

class EvidenceMemoryItem(BaseModel):
    id: str
    sourceId: str
    sourceType: str
    eventType: str
    title: str
    description: str | None = None
    eventDate: str | None = None

class RecommendationPreferences(BaseModel):
    preferredDurationMinutes: int | None = None
    environment: str | None = None # Indoor, Outdoor, Any
    format: str | None = None # Individual, Small Group, Whole Class
    category: str | None = None
    additionalNotes: str | None = None

class RecommendationRequest(BaseModel):
    scope: str # "Classroom" or "Child"
    targetName: str
    candidateExperiences: list[RecommendationCandidate]
    evidenceMemories: list[EvidenceMemoryItem]
    preferences: RecommendationPreferences | None = None

@app.post("/api/recommendations")
async def get_recommendations(req: RecommendationRequest, x_tenant_api_key: str | None = Header(default=None)):
    if not x_tenant_api_key:
        raise HTTPException(status_code=401, detail="X-Tenant-Api-Key header is missing. Please configure your AI API key in Settings.")

    if not req.candidateExperiences:
        return {"recommendations": []}

    try:
        genai.configure(api_key=x_tenant_api_key)
        model = genai.GenerativeModel("gemini-3.5-flash", system_instruction=RECOMMENDATIONS_INSTRUCTION)

        context_payload = {
            "scope": req.scope,
            "targetName": req.targetName,
            "preferences": req.preferences.dict() if req.preferences else {},
            "evidenceMemories": [m.dict() for m in req.evidenceMemories[:30]], # Top relevant memories
            "candidateExperiences": [c.dict() for c in req.candidateExperiences[:25]] # Eligible candidates
        }

        prompt = f"Please recommend suitable experiences using this context and evidence:\n{json.dumps(context_payload, indent=2)}"
        response = model.generate_content(prompt)
        text = response.text.strip()

        if text.startswith("```json"):
            text = text[7:]
        if text.startswith("```"):
            text = text[3:]
        if text.endswith("```"):
            text = text[:-3]

        recs = json.loads(text.strip())
        if not isinstance(recs, list):
            recs = recs.get("recommendations", [])

        # Post-validation: ensure all recommended IDs actually exist in candidates
        valid_ids = {c.id for c in req.candidateExperiences}
        valid_recs = [r for r in recs if isinstance(r, dict) and r.get("experienceId") in valid_ids]

        return {"recommendations": valid_recs}
    except Exception as e:
        print(f"Error generating recommendations: {e}")
        # Deterministic grounded fallback
        fallback = []
        for c in req.candidateExperiences[:3]:
            fallback.append({
                "experienceId": c.id,
                "experienceTitle": c.title,
                "suggestedFormat": "Whole Class" if req.scope == "Classroom" else "Individual",
                "estimatedDurationMinutes": 20,
                "rationale": f"Suggested experience from your active catalog suitable for {req.targetName}.",
                "supportingEvidence": [],
                "materials": c.materials,
                "isEvidenceLimited": True
            })
        return {"recommendations": fallback}

JOURNEY_INSIGHTS_INSTRUCTION = """You are an early childhood education specialist supporting teachers in reviewing child progress.
Your task is to analyze recorded child journey observations, session participations, and teacher feedback for a specific child during a selected reporting period.

CRITICAL INSTRUCTIONS & SAFEGUARDS:
1. Grounding in Evidence: Use ONLY the provided 'evidenceMemories'. Every single statement you make must be directly backed by the evidence.
2. NO Hallucination: Do NOT invent observations, skills, developmental milestones, historical events, diagnoses, or parental feedback.
3. NO Clinical or Diagnostic Claims: Absolutely NO psychological, medical, or clinical diagnoses or unsupported developmental scores or rankings.
4. Clearly Distinguish Categories:
   - 'JourneySummary': Concise factual summary of recorded experiences and activities.
   - 'RecordedInterests': Interests explicitly supported by recorded teacher observations.
   - 'ParticipationPatterns': Factual patterns from recorded participation and feedback.
   - 'LearningOpportunities': Learning areas and experience opportunities the child had.
   - 'IdeasToConsider': Non-prescriptive ideas for future activities to explore.
5. Clearly Distinguish Classification:
   - 'RecordedFact': Direct facts from observations or sessions.
   - 'ObservedPattern': Observed patterns across multiple entries.
   - 'EducationalSuggestion': Educational ideas for teacher reflection.
6. Evidence Citations:
   - In each item's 'supportingEvidence', cite the exact 'sourceId', 'sourceType', 'title', and brief 'summary' from the evidence memories that substantiate the statement.
7. Evidence Gaps:
   - If records are limited, sparse, or missing for the reporting period, state so neutrally in 'evidenceGaps'. Do NOT treat missing records as a negative reflection on the child.
8. Future Activity Handoff:
   - For 'IdeasToConsider', whenever possible match suggestions to a provided candidate experience ID and title from 'candidateExperiences'.
9. Tone:
   - Neutral, respectful, encouraging, professional, and non-judgmental.

Return the result EXACTLY as a JSON object matching this schema:
{
  "overallSummary": "A concise, factual 2-3 sentence overview of recorded activities.",
  "evidenceGaps": "Note about any missing or limited records for this period, or null if sufficient records exist.",
  "items": [
    {
      "category": "JourneySummary" | "RecordedInterests" | "ParticipationPatterns" | "LearningOpportunities" | "IdeasToConsider",
      "classification": "RecordedFact" | "ObservedPattern" | "EducationalSuggestion",
      "statement": "Clear, grounded sentence.",
      "evidenceStrength": "Strong" | "Supported" | "Emerging" | "Limited",
      "supportingEvidence": [
        {
          "sourceId": "UUID string from evidence",
          "sourceType": "child_stories" | "child_journey_entries" | "experience_participations",
          "title": "Title of record",
          "summary": "Brief explanation of what was recorded"
        }
      ],
      "suggestedExperienceId": "UUID string matching candidate experience if applicable or null",
      "suggestedExperienceTitle": "Title matching candidate experience if applicable or null",
      "rankOrder": 1
    }
  ]
}
Do NOT use markdown code blocks (```json) in your response. Output only raw JSON.
"""

class JourneyInsightRequest(BaseModel):
    childId: str
    childName: str
    periodType: str
    periodStartDate: str
    periodEndDate: str
    evidenceMemories: list[EvidenceMemoryItem]
    candidateExperiences: list[RecommendationCandidate] = []

@app.post("/api/journey-insights")
async def get_journey_insights(req: JourneyInsightRequest, x_tenant_api_key: str | None = Header(default=None)):
    if not x_tenant_api_key:
        raise HTTPException(status_code=401, detail="X-Tenant-Api-Key header is missing. Please configure your AI API key in Settings.")

    try:
        genai.configure(api_key=x_tenant_api_key)
        model = genai.GenerativeModel("gemini-3.5-flash", system_instruction=JOURNEY_INSIGHTS_INSTRUCTION)

        context_payload = {
            "childName": req.childName,
            "periodType": req.periodType,
            "periodStartDate": req.periodStartDate,
            "periodEndDate": req.periodEndDate,
            "evidenceCount": len(req.evidenceMemories),
            "evidenceMemories": [m.dict() for m in req.evidenceMemories[:40]],
            "candidateExperiences": [c.dict() for c in req.candidateExperiences[:20]]
        }

        prompt = f"Please generate evidence-grounded child journey insights for {req.childName} based on these records:\n{json.dumps(context_payload, indent=2)}"
        response = model.generate_content(prompt)
        text = response.text.strip()

        if text.startswith("```json"):
            text = text[7:]
        if text.startswith("```"):
            text = text[3:]
        if text.endswith("```"):
            text = text[:-3]

        parsed = json.loads(text.strip())
        
        # Post-validation: ensure valid candidate IDs if provided
        valid_exp_ids = {c.id for c in req.candidateExperiences}
        for item in parsed.get("items", []):
            exp_id = item.get("suggestedExperienceId")
            if exp_id and exp_id not in valid_exp_ids:
                item["suggestedExperienceId"] = None
                item["suggestedExperienceTitle"] = None

        return parsed
    except Exception as e:
        print(f"Error generating journey insights: {e}")
        # Deterministic grounded fallback
        fallback_items = []
        rank = 1
        
        if req.evidenceMemories:
            fallback_items.append({
                "category": "JourneySummary",
                "classification": "RecordedFact",
                "statement": f"{req.childName} participated in {len(req.evidenceMemories)} recorded learning moments and experiences during this period.",
                "evidenceStrength": "Supported",
                "supportingEvidence": [
                    {
                        "sourceId": req.evidenceMemories[0].sourceId,
                        "sourceType": req.evidenceMemories[0].sourceType,
                        "title": req.evidenceMemories[0].title,
                        "summary": req.evidenceMemories[0].description or req.evidenceMemories[0].title
                    }
                ],
                "suggestedExperienceId": None,
                "suggestedExperienceTitle": None,
                "rankOrder": rank
            })
            rank += 1

            # Extract an interest if available
            first_mem = req.evidenceMemories[0]
            fallback_items.append({
                "category": "RecordedInterests",
                "classification": "RecordedFact",
                "statement": f"Recorded observation indicates engaged interest in: {first_mem.title}.",
                "evidenceStrength": "Supported",
                "supportingEvidence": [
                    {
                        "sourceId": first_mem.sourceId,
                        "sourceType": first_mem.sourceType,
                        "title": first_mem.title,
                        "summary": first_mem.description or first_mem.title
                    }
                ],
                "suggestedExperienceId": None,
                "suggestedExperienceTitle": None,
                "rankOrder": rank
            })
            rank += 1
        else:
            fallback_items.append({
                "category": "JourneySummary",
                "classification": "RecordedFact",
                "statement": f"No active journey observations or completed experiences were recorded for {req.childName} during this period.",
                "evidenceStrength": "Limited",
                "supportingEvidence": [],
                "suggestedExperienceId": None,
                "suggestedExperienceTitle": None,
                "rankOrder": rank
            })
            rank += 1

        if req.candidateExperiences:
            top_cand = req.candidateExperiences[0]
            fallback_items.append({
                "category": "IdeasToConsider",
                "classification": "EducationalSuggestion",
                "statement": f"Consider exploring '{top_cand.title}' to offer hands-on engagement in {top_cand.category or 'creative play'}.",
                "evidenceStrength": "Emerging",
                "supportingEvidence": [],
                "suggestedExperienceId": top_cand.id,
                "suggestedExperienceTitle": top_cand.title,
                "rankOrder": rank
            })

        return {
            "overallSummary": f"Journey records show {len(req.evidenceMemories)} recorded moments between {req.periodStartDate} and {req.periodEndDate}.",
            "evidenceGaps": "Records are limited for this period; additional teacher observations will provide richer insights." if len(req.evidenceMemories) < 3 else None,
            "items": fallback_items
        }

WEEKLY_PLAN_INSTRUCTION = """You are an expert early childhood curriculum coordinator and planning assistant.
Your goal is to suggest a balanced, engaging weekly activity plan draft (Monday through Friday) for a specific classroom.

CRITICAL INSTRUCTIONS & SAFEGUARDS:
1. ONLY USE CANDIDATE EXPERIENCES: Every proposed daily activity MUST use an 'id' and 'title' from the supplied 'candidateExperiences'. Absolutely DO NOT invent experience IDs or create new master activities.
2. AVOID REPETITION: Check 'recentExperiences' and do NOT suggest activities that were already conducted in the last 14 days unless specifically requested.
3. LEARNING VARIETY: Balance experiences across the week:
   - Monday: Welcoming, constructive, or creative (e.g., building, tactile materials).
   - Tuesday: Storytelling, language, or communication.
   - Wednesday: Nature, sensory, discovery, or STEM.
   - Thursday: Music, movement, rhythm, or physical coordination.
   - Friday: Collaborative group art, celebration, or reflective play.
4. GROUNDED RATIONALE: State clearly why each activity fits the classroom age group, stated focus areas, and available materials.
5. RESPECT CONSTRAINTS: Respect indoor/outdoor preferences and material notes provided in 'preferences'.
6. FORMAT & TIMING: Default to Morning time slots (e.g. 20-30 min) and 'Small Group' or 'Whole Class' formats appropriate for daycare rhythm.
7. Tone: Encouraging, practical, respectful, and flexible for educators.

Return the result EXACTLY as a JSON object matching this schema:
{
  "weekSummary": "A brief 1-2 sentence overview of the pedagogical theme or focus for the week.",
  "dailyPlans": [
    {
      "dayOfWeek": "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday",
      "experienceId": "UUID string matching candidate experience",
      "experienceTitle": "Exact title matching candidate experience",
      "learningGoal": "Clear, developmentally appropriate objective",
      "groupFormat": "Small Group" | "Whole Class" | "Individual",
      "estimatedDurationMinutes": 25,
      "timeOfDay": "Morning" | "Afternoon",
      "requiredMaterials": "Key materials needed",
      "rationale": "Evidence-grounded reason for selecting this activity for this day"
    }
  ]
}
Do NOT use markdown code blocks (```json). Return only raw JSON.
"""

class WeeklyPlanPreferences(BaseModel):
    focusAreas: str | None = None
    indoorOutdoorPreference: str = "Balanced"
    materialsNotes: str | None = None

class WeeklyPlanRequest(BaseModel):
    classroomId: str
    classroomName: str
    ageGroup: str | None = None
    weekStartDate: str
    weekEndDate: str
    candidateExperiences: list[RecommendationCandidate]
    recentExperiences: list[str] = [] # Titles or IDs recently completed
    preferences: WeeklyPlanPreferences | None = None

@app.post("/api/weekly-plan")
async def get_weekly_plan(req: WeeklyPlanRequest, x_tenant_api_key: str | None = Header(default=None)):
    if not x_tenant_api_key:
        raise HTTPException(status_code=401, detail="X-Tenant-Api-Key header is missing. Please configure your AI API key in Settings.")

    if not req.candidateExperiences:
        return {"weekSummary": "No candidate experiences available.", "dailyPlans": []}

    try:
        genai.configure(api_key=x_tenant_api_key)
        model = genai.GenerativeModel("gemini-3.5-flash", system_instruction=WEEKLY_PLAN_INSTRUCTION)

        context_payload = {
            "classroomName": req.classroomName,
            "ageGroup": req.ageGroup or "Toddler / Pre-K",
            "weekStartDate": req.weekStartDate,
            "weekEndDate": req.weekEndDate,
            "preferences": req.preferences.dict() if req.preferences else {},
            "recentExperiences": req.recentExperiences[:15],
            "candidateExperiences": [c.dict() for c in req.candidateExperiences[:30]]
        }

        prompt = f"Please generate a balanced Monday-Friday weekly plan draft using these candidate experiences:\n{json.dumps(context_payload, indent=2)}"
        response = model.generate_content(prompt)
        text = response.text.strip()

        if text.startswith("```json"):
            text = text[7:]
        if text.startswith("```"):
            text = text[3:]
        if text.endswith("```"):
            text = text[:-3]

        parsed = json.loads(text.strip())

        # Post-validation: ensure all proposed experienceIds exist in candidates
        valid_map = {c.id: c.title for c in req.candidateExperiences}
        valid_plans = []
        days_order = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]

        for item in parsed.get("dailyPlans", []):
            exp_id = item.get("experienceId")
            if exp_id in valid_map:
                item["experienceTitle"] = valid_map[exp_id]
                valid_plans.append(item)

        # If model returned fewer than 5 days, backfill missing days deterministically
        seen_days = {p.get("dayOfWeek") for p in valid_plans}
        cand_idx = 0
        for day in days_order:
            if day not in seen_days:
                # Find an unused candidate
                cand = req.candidateExperiences[cand_idx % len(req.candidateExperiences)]
                cand_idx += 1
                valid_plans.append({
                    "dayOfWeek": day,
                    "experienceId": cand.id,
                    "experienceTitle": cand.title,
                    "learningGoal": f"Exploration and engagement in {cand.category or 'hands-on discovery'}.",
                    "groupFormat": "Small Group",
                    "estimatedDurationMinutes": 25,
                    "timeOfDay": "Morning",
                    "requiredMaterials": ", ".join(cand.materials) if cand.materials else "Standard classroom materials",
                    "rationale": f"Suggested experience from your library suitable for {req.classroomName}."
                })

        return {
            "weekSummary": parsed.get("weekSummary", f"Weekly theme for {req.classroomName} balancing creative play, movement, and discovery."),
            "dailyPlans": valid_plans
        }
    except Exception as e:
        print(f"Error generating weekly plan: {e}")
        # Deterministic grounded fallback
        days_order = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
        fallback_plans = []
        for i, day in enumerate(days_order):
            cand = req.candidateExperiences[i % len(req.candidateExperiences)]
            fallback_plans.append({
                "dayOfWeek": day,
                "experienceId": cand.id,
                "experienceTitle": cand.title,
                "learningGoal": f"Hands-on exploration in {cand.category or 'learning and play'}.",
                "groupFormat": "Small Group",
                "estimatedDurationMinutes": 25,
                "timeOfDay": "Morning",
                "requiredMaterials": ", ".join(cand.materials) if cand.materials else "Classroom learning materials",
                "rationale": f"Candidate experience matching age group {req.ageGroup or 'active classroom'}."
            })

        return {
            "weekSummary": f"Balanced weekly plan draft for {req.classroomName} focusing on play and exploration.",
            "dailyPlans": fallback_plans
        }

PARENT_COMMUNICATION_INSTRUCTION = """You are an experienced, compassionate early childhood educator assistant for EnjoyEveryday daycare SaaS.
Your goal is to draft a warm, respectful, parent-friendly communication update for a child's family based STRICTLY on recorded daycare evidence.

CRITICAL GROUNDING AND PRIVACY RULES:
1. ONLY refer to activities, moments, meals, naps, and milestones provided in the context. NEVER invent experiences, quotes, accomplishments, or developmental claims.
2. If evidence is minimal or not recorded for a section, do not fabricate details. Explain gently what was engaged in or that quiet rest/play was observed.
3. NEVER compare children with others or diagnose medical/developmental conditions.
4. Keep the tone warm, celebratory, clear, and professional. Avoid internal jargon or acronyms.
5. Provide:
   - "title": A warm subject line (e.g., "Aarav's Day: Building Tall Towers & Outdoor Joy" or "Weekly Learning Highlights for Aarav")
   - "greeting": A personal opening (e.g., "Dear Sunita,")
   - "highlights": 1-2 friendly paragraphs detailing what the child engaged in today/this week.
   - "growthAndLearning": What skills or curiosity this experience encouraged (e.g. sharing, fine-motor coordination, vocabulary).
   - "familyConversationStarter": A fun, simple open-ended question the family can ask at dinner or bedtime (e.g., "Ask Aarav about the giant block tower we built together this morning!").
   - "signOff": Warm closing from the classroom team.

Return the result EXACTLY as a JSON object:
{
  "title": "...",
  "greeting": "...",
  "highlights": "...",
  "growthAndLearning": "...",
  "familyConversationStarter": "...",
  "signOff": "Warmly,\\nThe Classroom Team"
}
Do NOT use markdown code blocks (```json) in your response. Just output the raw JSON object.
"""

class ParentCommunicationRequest(BaseModel):
    childName: str
    classroomName: str = "Classroom"
    recipientName: str = "Parent"
    recipientRelationship: str = "Parent"
    communicationType: str = "DailyUpdate"
    reportingPeriod: str = "Today"
    attendanceSummary: str = ""
    completedExperiences: list[str] = []
    approvedMoments: list[str] = []
    careHighlights: list[str] = []
    milestoneHighlights: list[str] = []
    relevantEvents: list[str] = []
    teacherNotes: str = ""

@app.post("/api/parent-communication")
def generate_parent_communication(req: ParentCommunicationRequest, x_tenant_api_key: str = Header(None)):
    child_name = req.childName or "Your child"
    parent_greeting = f"Dear {req.recipientName}," if req.recipientName else f"Dear {child_name}'s Family,"
    
    # Grounded fallback draft builder
    def build_grounded_fallback():
        highlights_parts = []
        if req.attendanceSummary:
            highlights_parts.append(f"{child_name} was welcomed into our classroom ({req.attendanceSummary}).")
        if req.completedExperiences:
            exp_text = "; ".join(req.completedExperiences[:3])
            highlights_parts.append(f"During group experiences, {child_name} participated with interest in: {exp_text}.")
        if req.approvedMoments:
            mom_text = "; ".join(req.approvedMoments[:3])
            highlights_parts.append(f"A wonderful moment we observed: {mom_text}.")
        if req.careHighlights:
            care_text = "; ".join(req.careHighlights[:3])
            highlights_parts.append(f"Care & daily rhythm highlights: {care_text}.")
        if not highlights_parts:
            highlights_parts.append(f"{child_name} had a calm and gentle day participating in classroom play and social interactions.")

        highlights = " ".join(highlights_parts)
        
        learning_parts = []
        if req.milestoneHighlights:
            learning_parts.append(f"Celebrated milestone: {'; '.join(req.milestoneHighlights)}.")
        if req.completedExperiences:
            learning_parts.append(f"These experiences supported hands-on discovery, social cooperation, and fine-motor expression.")
        else:
            learning_parts.append(f"Today's play supported communication, peer relationships, and natural curiosity.")
        growth = " ".join(learning_parts)

        # Family conversation starter
        starter = f"Ask {child_name} about their favorite thing they explored or played with friends today!"
        if req.completedExperiences:
            first_exp = req.completedExperiences[0]
            starter = f"Ask {child_name} what they enjoyed most during our '{first_exp}' exploration today!"

        title = f"{child_name}'s Daily Joy & Learning" if req.communicationType == "DailyUpdate" else f"{child_name}'s Weekly Learning Highlights"

        return {
            "title": title,
            "greeting": parent_greeting,
            "highlights": highlights,
            "growthAndLearning": growth,
            "familyConversationStarter": starter,
            "signOff": f"Warmly,\nThe {req.classroomName} Team"
        }

    try:
        if not x_tenant_api_key:
            return build_grounded_fallback()

        genai.configure(api_key=x_tenant_api_key)
        model = genai.GenerativeModel("gemini-1.5-flash", system_instruction=PARENT_COMMUNICATION_INSTRUCTION)

        context_payload = {
            "childName": child_name,
            "classroomName": req.classroomName,
            "recipientName": req.recipientName,
            "recipientRelationship": req.recipientRelationship,
            "communicationType": req.communicationType,
            "reportingPeriod": req.reportingPeriod,
            "attendanceSummary": req.attendanceSummary,
            "completedExperiences": req.completedExperiences[:5],
            "approvedMoments": req.approvedMoments[:5],
            "careHighlights": req.careHighlights[:5],
            "milestoneHighlights": req.milestoneHighlights[:3],
            "relevantEvents": req.relevantEvents[:3],
            "teacherNotes": req.teacherNotes
        }

        prompt = f"Please prepare a parent communication update based strictly on this verified evidence:\n{json.dumps(context_payload, indent=2)}"
        response = model.generate_content(prompt)
        text = response.text.strip()

        if text.startswith("```json"):
            text = text[7:]
        if text.startswith("```"):
            text = text[3:]
        if text.endswith("```"):
            text = text[:-3]

        parsed = json.loads(text.strip())
        return {
            "title": parsed.get("title", f"{child_name}'s Learning Update"),
            "greeting": parsed.get("greeting", parent_greeting),
            "highlights": parsed.get("highlights", ""),
            "growthAndLearning": parsed.get("growthAndLearning", ""),
            "familyConversationStarter": parsed.get("familyConversationStarter", ""),
            "signOff": parsed.get("signOff", f"Warmly,\nThe {req.classroomName} Team")
        }
    except Exception as e:
        print(f"Error in parent communication AI: {e}")
        return build_grounded_fallback()

ORGANIZATION_INTELLIGENCE_INSTRUCTION = """You are a senior operational and pedagogical advisor for early childhood daycare management in the EnjoyEveryday SaaS.
You receive structured aggregate operational metrics (attendance rate, planned vs completed experiences, curriculum domain distribution, planning gaps, data completeness score).

CRITICAL GROUNDING AND SAFETY RULES:
1. Ground every statement STRICTLY in the provided numbers. NEVER invent metrics, percentages, or statistics.
2. NEVER diagnose children, rank teachers or children, or make disciplinary/employment recommendations.
3. Distinguish between actual operational patterns and data limitations (e.g. if records were not logged).
4. Provide structured, actionable, evidence-based insights with:
   - "title": Concise, informative title
   - "category": One of OperationalOverview, CurriculumVariety, PlanningGaps, DataQuality, Engagement
   - "observation": What the data shows (factual statement)
   - "possibleExplanation": Why this might be happening (clearly marked as an interpretation)
   - "suggestedNextStep": Practical action for daycare management or pedagogical director
   - "dataLimitations": Any gaps in the data sample

Return the result EXACTLY as a JSON array of objects:
[
  {
    "title": "...",
    "category": "...",
    "observation": "...",
    "possibleExplanation": "...",
    "suggestedNextStep": "...",
    "dataLimitations": "..."
  }
]
Do NOT use markdown code blocks (```json) in your response. Just output the raw JSON array.
"""

class OrganizationIntelligenceRequest(BaseModel):
    scopeName: str = "Organization"
    reportingPeriod: str = "Last7Days"
    attendanceRate: float = 100.0
    planningCoverage: float = 0.0
    completedExperiences: int = 0
    upcomingExperiences: int = 0
    uniqueExperiences: int = 0
    varietyDiversityRate: float = 100.0
    domainBreakdown: dict[str, int] = {}
    classroomsNeedingPlansCount: int = 0
    classroomsNeedingPlans: list[str] = []
    missingDeparturesCount: int = 0
    pendingDraftsCount: int = 0
    dataCompletenessScore: float = 0.0

@app.post("/api/organization-intelligence")
def generate_organization_intelligence(req: OrganizationIntelligenceRequest, x_tenant_api_key: str = Header(None)):
    def build_grounded_fallback():
        insights = []
        # Insight 1: Operational Attendance & Activity
        att_obs = f"Attendance rate is currently {req.attendanceRate}%, with {req.completedExperiences} completed experiences and {req.upcomingExperiences} planned across {req.scopeName}."
        insights.append({
            "title": "Operational Rhythm & Attendance Health",
            "category": "OperationalOverview",
            "observation": att_obs,
            "possibleExplanation": "Stable attendance indicates predictable classroom routine and reliable family engagement.",
            "suggestedNextStep": "Continue reviewing morning arrival peaks to optimize staffing and classroom welcome moments.",
            "dataLimitations": "Data reflects records logged during the active reporting period."
        })

        # Insight 2: Curriculum Variety
        top_domain = max(req.domainBreakdown, key=req.domainBreakdown.get) if req.domainBreakdown else "Hands-On Play"
        var_obs = f"Classrooms engaged in {req.uniqueExperiences} distinct learning experiences with a diversity score of {req.varietyDiversityRate}%. The most active area is {top_domain}."
        insights.append({
            "title": "Curriculum Balance & Learning Domains",
            "category": "CurriculumVariety",
            "observation": var_obs,
            "possibleExplanation": "Educators have maintained an active repertoire with strong emphasis on creative and cognitive exploration.",
            "suggestedNextStep": "Encourage teachers to incorporate more outdoor nature and movement activities into next week's schedule.",
            "dataLimitations": "Domain distribution is calculated based on planned experience tags."
        })

        # Insight 3: Planning Gaps
        if req.classroomsNeedingPlansCount > 0:
            cls_names = ", ".join(req.classroomsNeedingPlans[:3])
            gap_obs = f"{req.classroomsNeedingPlansCount} classroom(s) ({cls_names}) have fewer than 2 experiences scheduled for the upcoming week."
            insights.append({
                "title": "Advance Planning Readiness",
                "category": "PlanningGaps",
                "observation": gap_obs,
                "possibleExplanation": "Teachers may still be finalizing lesson choices or drafting routines manually.",
                "suggestedNextStep": "Prompt classroom lead educators to use the AI Weekly Planning Assistant to draft balanced 5-day schedules.",
                "dataLimitations": "Does not account for unrecorded informal daily play or seasonal closures."
            })

        # Insight 4: Data Quality & Operational Hygiene
        act_obs = f"Identified {req.missingDeparturesCount} departure checks without checkout timestamps and {req.pendingDraftsCount} parent communications awaiting review."
        insights.append({
            "title": "Operational Record Completeness",
            "category": "DataQuality",
            "observation": act_obs,
            "possibleExplanation": "Busy afternoon transitions can sometimes lead to deferred departure sign-outs or delayed draft approvals.",
            "suggestedNextStep": "Brief classroom teams during afternoon handover to ensure children are signed out and parent communications reviewed.",
            "dataLimitations": "Calculated based on attendance check-in timestamps and parent draft workflow statuses."
        })

        return insights

    try:
        if not x_tenant_api_key:
            return build_grounded_fallback()

        genai.configure(api_key=x_tenant_api_key)
        model = genai.GenerativeModel("gemini-1.5-flash", system_instruction=ORGANIZATION_INTELLIGENCE_INSTRUCTION)

        context_payload = req.dict()
        prompt = f"Please provide executive-level organization intelligence insights based strictly on these metrics:\n{json.dumps(context_payload, indent=2)}"
        response = model.generate_content(prompt)
        text = response.text.strip()

        if text.startswith("```json"):
            text = text[7:]
        if text.startswith("```"):
            text = text[3:]
        if text.endswith("```"):
            text = text[:-3]

        parsed = json.loads(text.strip())
        if isinstance(parsed, list) and len(parsed) > 0:
            return parsed
        return build_grounded_fallback()
    except Exception as e:
        print(f"Error in organization intelligence AI: {e}")
        return build_grounded_fallback()

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)


