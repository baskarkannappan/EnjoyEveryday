-- Rollback migration 076
DELETE FROM role_permissions WHERE role_id = 'a0000000-0000-0000-0000-000000000010'::uuid;
DELETE FROM role_permissions WHERE role_id IN (SELECT id FROM roles WHERE name = 'LeadTeacher');
DELETE FROM roles WHERE name = 'LeadTeacher';
