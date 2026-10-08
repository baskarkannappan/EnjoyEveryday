# EnjoyEveryday AI Agent

This project contains the AI Agent for the EnjoyEveryday application. It is built using [FastAPI](https://fastapi.tiangolo.com/) and uses the [Google Generative AI (Gemini)](https://ai.google.dev/) API to generate and improve experience ideas.

## Prerequisites

- Python 3.10 or higher installed.

## Setup Instructions

1. **Navigate to the AI Agent Directory**
   Open your terminal and navigate to this folder:
   ```powershell
   cd c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.AiAgent
   ```

2. **Create a Virtual Environment**
   It is recommended to run this within a virtual environment.
   ```powershell
   python -m venv venv
   ```

3. **Activate the Virtual Environment**
   - On **Windows** (PowerShell):
     ```powershell
     .\venv\Scripts\Activate.ps1
     ```
   - On **macOS/Linux**:
     ```bash
     source venv/bin/activate
     ```

4. **Install Dependencies**
   Install the required Python packages using `pip`.
   ```powershell
   pip install fastapi uvicorn pydantic google-generativeai
   ```

## Running the Application

Once your dependencies are installed, you can start the application using `uvicorn`:

```powershell
python main.py
```
*Alternatively, you can run uvicorn directly:*
```powershell
uvicorn main:app --host 127.0.0.1 --port 8000 --reload
```

The server will start at `http://127.0.0.1:8000`.

## API Keys
The application expects an `X-Tenant-Api-Key` header with a valid Google Gemini API key to process AI requests. This should be configured within the main application settings.

## API Endpoints
- `POST /api/ideas`: Generates initial experience ideas based on a prompt.
- `POST /api/improve`: Improves an existing experience's sections (such as Challenge, Magic Moment, etc.) based on user requests.
