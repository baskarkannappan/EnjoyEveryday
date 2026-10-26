-- Trigger: audit_log_no_truncate
-- Backup generated on 2026-10-25 20:34:15

CREATE TRIGGER audit_log_no_truncate BEFORE TRUNCATE ON public.audit_log FOR EACH STATEMENT EXECUTE FUNCTION audit_log_append_only();
