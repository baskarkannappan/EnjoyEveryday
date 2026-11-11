-- Migration 110: Prevent Duplicate Marketplace Experience Library Entries
-- Safely consolidates existing duplicate marketplace entries, preserves all execution history,
-- adds platform_experience_id column, and creates a unique index on (organization_id, platform_experience_id).

-- Step 1: Safely consolidate all existing duplicate marketplace imports across organizations
DO $$
DECLARE
    rec RECORD;
    v_canonical_id UUID;
    v_dup_id UUID;
BEGIN
    FOR rec IN 
        SELECT 
            organization_id, 
            (dna_payload->'_platformProvenance'->>'PlatformExperienceId')::uuid AS platform_exp_id
        FROM experiences
        WHERE organization_id IS NOT NULL 
          AND dna_payload->'_platformProvenance'->>'PlatformExperienceId' IS NOT NULL
        GROUP BY organization_id, (dna_payload->'_platformProvenance'->>'PlatformExperienceId')::uuid
        HAVING COUNT(*) > 1
    LOOP
        -- Choose canonical ID with the most activity (sessions, schedules, feedback, or earliest created)
        SELECT e.id INTO v_canonical_id
        FROM experiences e
        LEFT JOIN (
            SELECT experience_id, COUNT(*) as cnt 
            FROM experience_sessions 
            GROUP BY experience_id
        ) s ON s.experience_id = e.id
        LEFT JOIN (
            SELECT experience_id, COUNT(*) as cnt 
            FROM experience_schedules 
            GROUP BY experience_id
        ) sc ON sc.experience_id = e.id
        WHERE e.organization_id = rec.organization_id
          AND (e.dna_payload->'_platformProvenance'->>'PlatformExperienceId')::uuid = rec.platform_exp_id
        ORDER BY COALESCE(s.cnt, 0) + COALESCE(sc.cnt, 0) DESC, e.created_at ASC
        LIMIT 1;

        -- Re-link all referencing tables for other duplicate IDs to the canonical ID
        FOR v_dup_id IN 
            SELECT id 
            FROM experiences 
            WHERE organization_id = rec.organization_id
              AND (dna_payload->'_platformProvenance'->>'PlatformExperienceId')::uuid = rec.platform_exp_id
              AND id <> v_canonical_id
        LOOP
            UPDATE experience_sessions SET experience_id = v_canonical_id WHERE experience_id = v_dup_id;
            UPDATE experience_schedules SET experience_id = v_canonical_id WHERE experience_id = v_dup_id;
            UPDATE experience_feedback SET experience_id = v_canonical_id WHERE experience_id = v_dup_id;
            UPDATE child_journey_entries SET experience_id = v_canonical_id WHERE experience_id = v_dup_id;
            UPDATE event_experiences SET experience_id = v_canonical_id WHERE experience_id = v_dup_id;
            UPDATE experience_harvest_requests SET source_experience_id = v_canonical_id WHERE source_experience_id = v_dup_id;
            UPDATE experience_licenses SET experience_id = v_canonical_id WHERE experience_id = v_dup_id;
            UPDATE material_orders SET planned_experience_id = v_canonical_id WHERE planned_experience_id = v_dup_id;
            UPDATE weekly_planning_draft_items SET experience_id = v_canonical_id WHERE experience_id = v_dup_id;

            -- Remove duplicate version records and experience row
            DELETE FROM experience_versions WHERE experience_id = v_dup_id;
            DELETE FROM experiences WHERE id = v_dup_id;
        END LOOP;
    END LOOP;
END $$;

-- Step 2: Add platform_experience_id column to experiences table
ALTER TABLE experiences 
ADD COLUMN IF NOT EXISTS platform_experience_id uuid NULL 
REFERENCES platform_experiences(id) ON DELETE SET NULL;

-- Step 3: Backfill platform_experience_id for all existing imported experiences
UPDATE experiences
SET platform_experience_id = (dna_payload->'_platformProvenance'->>'PlatformExperienceId')::uuid
WHERE platform_experience_id IS NULL
  AND dna_payload->'_platformProvenance'->>'PlatformExperienceId' IS NOT NULL;

-- Step 4: Create unique index on (organization_id, platform_experience_id)
CREATE UNIQUE INDEX IF NOT EXISTS uq_experiences_org_platform_exp 
ON experiences (organization_id, platform_experience_id) 
WHERE platform_experience_id IS NOT NULL;

-- Step 5: Clean up any old function overloads to avoid PostgreSQL 42725 ambiguity
DROP FUNCTION IF EXISTS public.fn_create_experience(uuid, uuid, uuid, character varying, text, character varying, jsonb, uuid, timestamp with time zone, timestamp with time zone, uuid, uuid);
DROP FUNCTION IF EXISTS public.fn_create_experience(uuid, uuid, uuid, character varying, text, character varying, jsonb, uuid, timestamp with time zone, timestamp with time zone, uuid);

-- Step 6: Create single unambiguous fn_create_experience with standard 11 parameters
CREATE OR REPLACE FUNCTION public.fn_create_experience(
    p_id uuid,
    p_tenant_id uuid,
    p_organization_id uuid,
    p_title character varying,
    p_description text,
    p_status character varying,
    p_dna_payload jsonb,
    p_created_by_user_id uuid,
    p_created_at timestamp with time zone,
    p_updated_at timestamp with time zone,
    p_version_id uuid DEFAULT gen_random_uuid()
)
RETURNS TABLE(
    id uuid,
    tenant_id uuid,
    organization_id uuid,
    title character varying,
    description text,
    status character varying,
    dna_payload jsonb,
    created_by_user_id uuid,
    created_at timestamp with time zone,
    updated_at timestamp with time zone
)
LANGUAGE plpgsql
AS $function$
DECLARE
    v_platform_exp_id uuid;
BEGIN
    v_platform_exp_id := (p_dna_payload->'_platformProvenance'->>'PlatformExperienceId')::uuid;

    INSERT INTO experiences (
        id, tenant_id, organization_id, title, description, status, dna_payload, created_by_user_id, created_at, updated_at, platform_experience_id
    )
    VALUES (
        p_id, p_tenant_id, p_organization_id, p_title, p_description, p_status, p_dna_payload, p_created_by_user_id, p_created_at, p_updated_at, v_platform_exp_id
    );

    INSERT INTO experience_versions (
        id, experience_id, version_number, title, description, dna_payload, change_reason, modified_by_user_id, created_at
    )
    VALUES (
        p_version_id, p_id, 1, p_title, p_description, p_dna_payload, 'Initial Creation', p_created_by_user_id, p_created_at
    );

    RETURN QUERY
    SELECT e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload, e.created_by_user_id, e.created_at, e.updated_at
    FROM experiences e
    WHERE e.id = p_id;
END;
$function$;
