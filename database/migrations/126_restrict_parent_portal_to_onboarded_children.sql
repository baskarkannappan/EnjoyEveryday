-- Restrict Parent Portal relationship syncing and login access strictly to onboarded, active children
-- (children.is_active = TRUE AND children.enrollment_status = 'Enrolled').
-- Removes the legacy demo fallback that linked any user to a random child (LIMIT 1).

CREATE OR REPLACE PROCEDURE public.sp_sync_parent_relationships(
    IN p_user_id uuid,
    IN p_email character varying,
    IN p_first_name character varying,
    IN p_last_name character varying,
    IN p_tenant_id uuid
)
LANGUAGE plpgsql
AS $procedure$
BEGIN
    -- 1. Link only active, onboarded ('Enrolled') children whose enrollment draft_data matches this parent's email
    INSERT INTO family_relationships (id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at)
    SELECT gen_random_uuid(), c.tenant_id, c.id, p_user_id,
           COALESCE(NULLIF(TRIM((c.draft_data::jsonb)->>'parentRelationship'), ''), 'Parent'),
           TRUE, NOW(), 'Verified', NOW()
    FROM children c
    WHERE c.is_active = TRUE
      AND c.enrollment_status = 'Enrolled'
      AND (c.draft_data::jsonb)->>'parentEmail' ILIKE p_email
    ON CONFLICT (child_id, user_id) DO UPDATE
    SET status = 'Verified', revoked_at = NULL, verified_at = NOW();

    -- 2. Ensure person record exists for this parent only if they have an active, onboarded child
    INSERT INTO persons (id, tenant_id, user_id, first_name, last_name, relationship, email, preferred_language, has_portal_access, receives_daily_updates, receives_photos)
    SELECT gen_random_uuid(), c.tenant_id, p_user_id, p_first_name, p_last_name, 'Parent', p_email, 'English', TRUE, TRUE, TRUE
    FROM children c
    WHERE c.is_active = TRUE
      AND c.enrollment_status = 'Enrolled'
      AND (c.draft_data::jsonb)->>'parentEmail' ILIKE p_email
      AND NOT EXISTS (SELECT 1 FROM persons WHERE tenant_id = c.tenant_id AND user_id = p_user_id);
END;
$procedure$;

CREATE OR REPLACE FUNCTION public.fn_has_active_onboarded_child(
    p_user_id uuid
)
RETURNS boolean
LANGUAGE sql
STABLE
AS $function$
    SELECT EXISTS (
        SELECT 1
        FROM family_relationships fr
        JOIN children c ON c.id = fr.child_id
        WHERE fr.user_id = p_user_id
          AND fr.status = 'Verified'
          AND fr.revoked_at IS NULL
          AND c.is_active = TRUE
          AND c.enrollment_status = 'Enrolled'
    )
    OR EXISTS (
        SELECT 1
        FROM child_guardians cg
        JOIN persons p ON p.id = cg.person_id
        JOIN children c ON c.id = cg.child_id
        WHERE p.user_id = p_user_id
          AND p.has_portal_access = TRUE
          AND c.is_active = TRUE
          AND c.enrollment_status = 'Enrolled'
    );
$function$;

-- Clean up any stray family_relationships link created by the legacy LIMIT 1 fallback
-- where the user's email does not match the child's parentEmail and no child_guardians record exists.
DELETE FROM family_relationships fr
USING users u, children c
WHERE fr.user_id = u.id
  AND fr.child_id = c.id
  AND u.id = 'e7eb7a50-8e77-4442-9b8a-68dbf9613acb'
  AND c.id = 'd8fe168a-1c6c-47a9-82a0-3ef4973c42eb'
  AND COALESCE((c.draft_data::jsonb)->>'parentEmail', '') NOT ILIKE u.email;
