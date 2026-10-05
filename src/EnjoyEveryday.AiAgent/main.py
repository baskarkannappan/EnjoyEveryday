from fastapi import FastAPI, Header, HTTPException
from pydantic import BaseModel
import json
import uvicorn
import google.generativeai as genai

app = FastAPI(title="EnjoyEveryday AI Agent")

IDEAS_INSTRUCTION = """You are an expert early childhood education teacher.
The user will give you an idea for an experience for children (ages 3-5).
Suggest 3 possible directions for this idea.
Each direction must have a short title (with an emoji) and a short 1-2 sentence description.
Return the result EXACTLY as a JSON array of objects with 'title' and 'text' keys.
Example output:
[
  {"title": "☔ Rain Detectives", "text": "Listen, look and investigate what changes when rain arrives."},
  {"title": "💧 Build a Water World", "text": "Create rivers, bridges and little worlds using simple materials."}
]
Do NOT use markdown code blocks (```json) in your response. Just output the raw array.
"""

IMPROVE_INSTRUCTION = """You are an expert early childhood education teacher.
The user will give you a JSON representation of an existing Experience design (DnaPayload) and a specific improvement request like 'Make it more playful' or 'Add more child choice'.
Your job is to identify EXACTLY ONE section to improve that will have the biggest impact for the request.
Do NOT rewrite the entire experience. Just suggest one precise change.

Return the result EXACTLY as a JSON object matching this structure:
{
  "action": "Improve",
  "targetSection": "ChildChoice", 
  "currentContent": "The original text you are replacing",
  "suggestedContent": "The new text you are suggesting",
  "reason": "Why this improves the experience based on the request.",
  "confidence": 0.95,
  "requiresApproval": true
}
Do NOT use markdown code blocks (```json) in your response. Just output the raw JSON object.
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
        return {"suggestions": [
            {"title": "🌟 Explorers", "text": f"Explore {req.idea} together."},
            {"title": "🎨 Creators", "text": f"Make something inspired by {req.idea}."}
        ]}

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
          "action": "Improve",
          "targetSection": "Challenge",
          "currentContent": "",
          "suggestedContent": "Add more interactive elements here.",
          "reason": "Fallback suggestion due to error.",
          "confidence": 0.5,
          "requiresApproval": True
        }

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)
