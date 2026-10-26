-- Function: public.trg_tenants_ensure_roles
-- Backup generated on 2026-10-25 20:34:15

CREATE OR REPLACE FUNCTION public.trg_tenants_ensure_roles()  RETURNS trigger  LANGUAGE plpgsql AS $function$ BEGIN     PERFORM ensure_tenant_roles(NEW.id);     RETURN NEW; END $function$ ;
