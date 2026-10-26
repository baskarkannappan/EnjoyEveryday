-- Trigger: tenants_ensure_roles
-- Backup generated on 2026-10-25 20:34:15

CREATE TRIGGER tenants_ensure_roles AFTER INSERT ON public.tenants FOR EACH ROW EXECUTE FUNCTION trg_tenants_ensure_roles();
