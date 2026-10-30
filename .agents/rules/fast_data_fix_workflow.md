# Rule: Fast Data Diagnosis & Fix Workflow

When diagnosing or fixing data issues (especially when instructed not to touch the UI):

1. **Batch SQL Diagnostics in a Single Execution**:
   - Never run multiple round-trip `psql` commands sequentially for exploratory diagnostics.
   - Combine all diagnostic queries into one batched SQL block (or temporary script) so all relevant rows, counts, and function outputs return in a single call.

2. **Hypothesis-First Root Cause Pattern**:
   - When figures show `0/0`, missing counts, or unexpected multiples:
     1. Check date matching (`attendance_date`, `scheduled_date` vs `CURRENT_DATE`).
     2. Check active flags (`is_active = true`, `status != 'Inactive'`).
     3. Check system dummy records (e.g., virtual pool classrooms with 999 capacity).
     4. Check stored function `WHERE` clauses for missing tenant or active filters.

3. **Skip .NET Rebuilds for Pure Database & Data Fixes**:
   - PostgreSQL stored procedures, functions, schema migrations, and row updates take effect immediately at runtime.
   - Do NOT run `dotnet build` unless C# code or Razor files were actually modified.
