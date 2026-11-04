# Database Backup & Restore Guide

This guide explains how to back up your local Docker PostgreSQL database to GitHub and restore it on any machine.

---

## 1. Quick Backup & Push to Git

To create a fresh snapshot of your PostgreSQL database and automatically push it to GitHub:

```powershell
.\backup_db_to_git.ps1
```

### What this script does:
1. Runs `pg_dump` targeting the database `enjoyeveryday` at `localhost:5433`.
2. Includes `--clean --if-exists` so the backup is self-contained and clean when restored.
3. Saves the dump to `database_backups/enjoyeveryday_latest.sql`.
4. Staged, commits with a timestamp, and pushes to `origin/master`.

---

## 2. Restoring the Database

When setting up on a new laptop or recovering previous state:

### Step A: Verify Docker PostgreSQL is Running
Make sure your PostgreSQL container is running on port `5433`:
```powershell
docker ps
```

### Step B: Create the Database (If Fresh Setup)
If setting up a completely new environment where the database does not exist yet:
```powershell
$env:PGPASSWORD="postgres"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U postgres -c "CREATE DATABASE enjoyeveryday;"
```

### Step C: Restore from the Backup File
Run `psql` to restore the schema and data from `database_backups/enjoyeveryday_latest.sql`:

```powershell
$env:PGPASSWORD="postgres"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U postgres -d enjoyeveryday -f database_backups\enjoyeveryday_latest.sql
```

*(Note: If `psql.exe` is already in your system `PATH`, you can simply run `psql -h localhost -p 5433 -U postgres -d enjoyeveryday -f database_backups\enjoyeveryday_latest.sql`)*

---

## 3. Daily Automated Backup Setup (Windows Task Scheduler)

To configure Windows to run the backup script every night automatically:

1. Open **PowerShell as Administrator**.
2. Run the following command:

```powershell
$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\MyDrive\ProjectDrive\EnjoyEveryDay\backup_db_to_git.ps1" `
    -WorkingDirectory "C:\MyDrive\ProjectDrive\EnjoyEveryDay"

$trigger = New-ScheduledTaskTrigger -Daily -At 11:00PM

Register-ScheduledTask `
    -TaskName "EnjoyEveryday_Daily_DB_Backup" `
    -Action $action `
    -Trigger $trigger `
    -Description "Daily automated PostgreSQL dump & Git push for EnjoyEveryday"
```

### To Verify or Modify the Scheduled Task:
- Press `Win + R`, type `taskschd.msc`, and press Enter.
- Look for `EnjoyEveryday_Daily_DB_Backup` in the Task Scheduler Library.
- You can right-click it and choose **Run** anytime to test it manually.
