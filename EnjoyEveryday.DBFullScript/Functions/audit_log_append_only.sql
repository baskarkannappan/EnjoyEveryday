-- Function: public.audit_log_append_only
-- Backup generated on 2026-10-25 20:32:52

CREATE OR REPLACE FUNCTION public.audit_log_append_only()  RETURNS trigger  LANGUAGE plpgsql AS $function$ BEGIN     RAISE EXCEPTION 'audit_log is append-only (% not permitted)', TG_OP USING ERRCODE = 'insufficient_privilege'; END $function$ ;
