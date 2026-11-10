@echo off
set PGPASSWORD=postgres
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U postgres -d enjoyeveryday %*

