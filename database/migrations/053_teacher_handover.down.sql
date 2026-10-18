-- 053_teacher_handover.down.sql
-- Rollback for Smart Daily Care & Teacher Handover

DROP TABLE IF EXISTS teacher_handovers CASCADE;

DELETE FROM role_permissions WHERE permission_id IN (
    SELECT id FROM permissions WHERE name = 'care.handover'
);
DELETE FROM permissions WHERE name = 'care.handover';
