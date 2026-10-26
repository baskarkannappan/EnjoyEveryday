-- Trigger: audit_log_no_change
-- Backup generated on 2026-10-25 20:34:15

CREATE TRIGGER audit_log_no_change BEFORE DELETE OR UPDATE ON public.audit_log FOR EACH ROW EXECUTE FUNCTION audit_log_append_only();
