-- 056_staff_roster_and_leave.down.sql
-- Rollback for Staff Roster, Leave & Coverage

DROP TABLE IF EXISTS staff_leaves CASCADE;
DROP TABLE IF EXISTS classroom_statutory_ratios CASCADE;

DELETE FROM role_permissions WHERE permission_id IN (
    SELECT id FROM permissions WHERE name IN ('staff.roster.view', 'staff.leave.manage')
);
DELETE FROM permissions WHERE name IN ('staff.roster.view', 'staff.leave.manage');
