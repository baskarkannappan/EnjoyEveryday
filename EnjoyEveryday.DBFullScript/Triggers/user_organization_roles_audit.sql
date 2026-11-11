-- Trigger: user_organization_roles_audit
-- Backup generated on 2026-11-11 09:06:14

CREATE TRIGGER user_organization_roles_audit AFTER INSERT OR DELETE ON public.user_organization_roles FOR EACH ROW EXECUTE FUNCTION audit_org_role_change();
