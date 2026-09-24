from fastapi import FastAPI
from pydantic import BaseModel
import json
import os
import uvicorn
from google.adk import Agent

app = FastAPI(title="EnjoyEveryday AI Agent")

# Set dummy API key if not set to prevent init errors in ADK
if "GOOGLE_API_KEY" not in os.environ and "GEMINI_API_KEY" not in os.environ:
    os.environ["GOOGLE_API_KEY"] = "dummy"

agent = Agent(
    name="educator",
    model="gemini-flash-latest",
    instruction="""You are an expert early childhood education teacher.
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
""",
)

class IdeaRequest(BaseModel):
    idea: str

class IdeaResponse(BaseModel):
    suggestions: list[dict]

@app.post("/api/ideas", response_model=IdeaResponse)
async def generate_ideas(req: IdeaRequest):
    try:
        # ADK Agent run method
        response = agent.run(req.idea)
        
        # Get the text from response
        # The ADK response object usually has a .text property or can be cast to str
        text = response.text if hasattr(response, 'text') else str(response)
        
        # Clean up possible markdown code blocks if the model ignored instructions
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
        # Fallback response
        return {"suggestions": [
            {"title": "🌟 Explorers", "text": f"Explore {req.idea} together."},
            {"title": "🎨 Creators", "text": f"Make something inspired by {req.idea}."}
        ]}

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)
