-- Migration 116: Prevent duplicate authorized pickup rows per child/person.
-- Root cause: sp_sync_parent_guardian used ON CONFLICT DO NOTHING without a unique constraint.

-- 1. Remove duplicates (keep the oldest row per child/person; re-point attendance references)
WITH ranked AS (
    SELECT id,
           FIRST_VALUE(id) OVER (PARTITION BY child_id, person_id ORDER BY is_active DESC, (pickup_code IS NULL), id) AS keep_id,
           ROW_NUMBER() OVER (PARTITION BY child_id, person_id ORDER BY is_active DESC, (pickup_code IS NULL), id) AS rn
    FROM authorized_pickups
    WHERE person_id IS NOT NULL
)
UPDATE child_attendance ca
SET authorized_pickup_id = r.keep_id
FROM ranked r
WHERE ca.authorized_pickup_id = r.id AND r.rn > 1;

DELETE FROM authorized_pickups ap
USING (
    SELECT id,
           ROW_NUMBER() OVER (PARTITION BY child_id, person_id ORDER BY is_active DESC, (pickup_code IS NULL), id) AS rn
    FROM authorized_pickups
    WHERE person_id IS NOT NULL
) d
WHERE ap.id = d.id AND d.rn > 1;

-- 2. Enforce uniqueness so ON CONFLICT DO NOTHING works
CREATE UNIQUE INDEX IF NOT EXISTS uq_authorized_pickups_child_person
    ON authorized_pickups (child_id, person_id)
    WHERE person_id IS NOT NULL;

