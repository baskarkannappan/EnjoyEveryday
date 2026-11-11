-- Trigger: audit_log_no_change
-- Backup generated on 2026-11-11 09:06:12

CREATE TRIGGER audit_log_no_change BEFORE DELETE OR UPDATE ON public.audit_log FOR EACH ROW EXECUTE FUNCTION audit_log_append_only();
