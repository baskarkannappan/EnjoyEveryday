-- 055_secure_pickup.down.sql
-- Rollback for Secure Pickup & Child Handover

ALTER TABLE child_attendance
DROP COLUMN IF EXISTS pickup_person_name,
DROP COLUMN IF EXISTS pickup_person_relationship,
DROP COLUMN IF EXISTS pickup_verification_method,
DROP COLUMN IF EXISTS pickup_notes,
DROP COLUMN IF EXISTS authorized_pickup_id,
DROP COLUMN IF EXISTS departure_verified_by_user_id;

DROP INDEX IF EXISTS idx_authorized_pickups_child;
DROP INDEX IF EXISTS idx_child_restrictions_child;
DROP INDEX IF EXISTS idx_child_attendance_departure;

DELETE FROM role_permissions WHERE permission_id IN (
    SELECT id FROM permissions WHERE name = 'attendance.pickup.verify'
);
DELETE FROM permissions WHERE name = 'attendance.pickup.verify';
