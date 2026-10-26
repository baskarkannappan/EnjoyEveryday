-- Function: public.audit_org_role_change
-- Backup generated on 2026-10-25 20:32:52

CREATE OR REPLACE FUNCTION public.audit_org_role_change()  RETURNS trigger  LANGUAGE plpgsql AS $function$ BEGIN     IF TG_OP = 'INSERT' THEN         INSERT INTO audit_log (tenant_id, action, entity_type, entity_id, details)         VALUES (NEW.tenant_id, 'Role.Granted.Db', 'User', NEW.user_id,                 jsonb_build_object('role_id', NEW.role_id, 'organization_id', NEW.organization_id));         RETURN NEW;     END IF;     INSERT INTO audit_log (tenant_id, action, entity_type, entity_id, details)     VALUES (OLD.tenant_id, 'Role.Revoked.Db', 'User', OLD.user_id,             jsonb_build_object('role_id', OLD.role_id, 'organization_id', OLD.organization_id));     RETURN OLD; END $function$ ;
