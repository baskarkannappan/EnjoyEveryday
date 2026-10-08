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

class MaterialsRequest(BaseModel):
    experienceDna: dict

@app.post("/api/materials")
async def suggest_materials(req: MaterialsRequest, x_tenant_api_key: str | None = Header(default=None)):
    if not x_tenant_api_key:
        raise HTTPException(status_code=401, detail="X-Tenant-Api-Key header is missing. Please configure your AI API key in Settings.")

    try:
        genai.configure(api_key=x_tenant_api_key)
        model = genai.GenerativeModel("gemini-3.5-flash", system_instruction=MATERIALS_INSTRUCTION)
        
        prompt = f"Experience DNA:\n{json.dumps(req.experienceDna, indent=2)}"
        response = model.generate_content(prompt)
        text = response.text
        
        if text.startswith("```json"):
            text = text[7:]
        if text.startswith("```"):
            text = text[3:]
        if text.endswith("```"):
            text = text[:-3]
            
        materials = json.loads(text.strip())
        if not isinstance(materials, list):
            if isinstance(materials, dict) and "materials" in materials:
                materials = materials["materials"]
            elif isinstance(materials, dict) and "Materials" in materials:
                materials = materials["Materials"]
            else:
                materials = []
                
        return {"Materials": materials}
    except Exception as e:
        print(f"Error suggesting materials: {e}")
        return {"Materials": []}

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)
