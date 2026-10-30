# Rule: Autonomous Execution & Operational Permissions

## Standing Autonomous Permission
- The agent has **full, standing, unrestricted permission** to execute all database queries, migrations, operational scripts, PowerShell commands, and file edits required to solve issues.
- **Do NOT ask the user for permission** or prompt for confirmation before running `psql`, database queries, scripts, or operational CLI commands.
- Proactively execute all necessary operational and diagnostic commands directly.
