# Visual Studio Developer Guide — Local Development & Debugging

This guide explains how to set up, run, and debug the **EnjoyEveryday** platform locally using **Visual Studio 2022** and **Docker Desktop** (for the database).

---

## 1. Prerequisites

Before starting, ensure your machine has the following tools installed:

| Tool | Required Version | Verification Command |
| :--- | :--- | :--- |
| **Visual Studio 2022** | v17.10+ (Community, Pro, or Enterprise) | Workload: *ASP.NET and web development* |
| **.NET SDK** | .NET 10.0+ | `dotnet --version` |
| **Python** | 3.12+ (for AI Agent) | `python --version` |
| **Docker Desktop** | Latest (WSL2 backend on Windows) | `docker --version` |

---

## 2. Step 1: Start the Local PostgreSQL Database

The PostgreSQL 15 database runs as a lightweight container in Docker Desktop so you don't need a heavy local PostgreSQL installation.

1. Ensure **Docker Desktop** is running.
2. Open a PowerShell terminal in the repository root and start the database:
   ```powershell
   docker compose up -d db
   ```
3. Verify the database is healthy and accepting connections:
   ```powershell
   scripts\db.cmd -c "SELECT current_database(), current_user, version();"
   ```

### Database Connection Parameters
* **Host**: `localhost`
* **Port**: `5433` *(mapped from container port 5432)*
* **Database**: `enjoyeveryday`
* **Username**: `postgres`
* **Password**: `postgres`

> [!NOTE]
> All database data is stored in a persistent Docker volume (`postgres_data`). Stopping or restarting the container does **not** wipe your data.

---

## 3. Step 2: Open and Run in Visual Studio 2022

1. Launch Visual Studio 2022 and open `EnjoyEveryday.slnx` from the repository root.
2. **Configure Multi-Project Startup**:
   * Right-click the Solution node in Solution Explorer &rarr; **Configure Startup Projects...**
   * Select **Multiple startup projects**.
   * Set:
     * `src\EnjoyEveryday.Api` &rarr; **Start**
     * `src\EnjoyEveryday.Daycare.Web` &rarr; **Start**
   * Click **Apply** and **OK**.
3. **Start Debugging**:
   * Press <kbd>F5</kbd> (or click **Start Debugging** with the green play arrow).
   * Visual Studio compiles both projects, launches the runtime servers, and opens your browser.

### Local Ports & Endpoints
* **Web Portal (Daycare)**: `https://localhost:7003` (or `http://localhost:5003`)
* **Core API & Swagger Docs**: `https://localhost:7013/swagger` (or `http://localhost:5165/swagger`)

---

## 4. Step 3: Run the AI Agent (Python FastAPI)

The AI module (`EnjoyEveryday.AiAgent`) is a lightweight Python FastAPI service powering Gemini ADK capabilities.

To run it alongside Visual Studio:

1. Open a terminal and navigate to the AI Agent directory:
   ```powershell
   cd src\EnjoyEveryday.AiAgent
   ```
2. Activate or create the virtual environment:
   ```powershell
   # If first time:
   python -m venv venv
   .\venv\Scripts\Activate.ps1
   pip install -r requirements.txt

   # If already created:
   .\venv\Scripts\Activate.ps1
   ```
3. Start the service with hot reload enabled:
   ```powershell
   uvicorn app.main:app --reload --port 8000
   ```
4. Access the interactive AI API docs at: `http://localhost:8000/docs`

---

## 5. Development Test Accounts

Use these pre-seeded accounts to log in and test different roles:

| Role | Email / Username | Organization | Description |
| :--- | :--- | :--- | :--- |
| **Daycare Admin** | `admin@littlestars.com` | Little Stars Daycare | Full daycare operations, 11 classrooms, 10 children. |
| **Org Admin** | `myassociationmanager005@gmail.com` | B_TEST_ORG / Little Stars | Multi-branch executive and organization governance. |

---

## 6. Daily Development Best Practices

* **Fast Targeted Builds**:
  Never run `dotnet build EnjoyEveryday.slnx` for single Razor or C# edits. Run targeted project builds:
  ```powershell
  dotnet build src/EnjoyEveryday.Daycare.Web/EnjoyEveryday.Daycare.Web.csproj --no-restore
  ```
  *(Completes in ~3s instead of 40s+)*.
* **Database Diagnosing**:
  Use the pre-configured script from any terminal:
  ```powershell
  scripts\db.cmd -c "<YOUR_SQL_QUERY>"
  ```
* **Testing Policy**:
  Do **not** execute automated test suites (`dotnet test`) during standard feature workflows unless explicitly requested.

