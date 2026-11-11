-- Trigger: audit_log_no_truncate
-- Backup generated on 2026-11-11 09:06:12

CREATE TRIGGER audit_log_no_truncate BEFORE TRUNCATE ON public.audit_log FOR EACH STATEMENT EXECUTE FUNCTION audit_log_append_only();
