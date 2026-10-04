ALTER TABLE teachers DROP CONSTRAINT IF EXISTS fk_teacher_tenant;
ALTER TABLE teachers ADD CONSTRAINT fk_teacher_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE;
