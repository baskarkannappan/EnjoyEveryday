-- Migration 142: Add branch_id and classroom_id scope columns to media_assets
-- Supports role-scoped photo downloads (Org Admin, Branch Admin, Lead Teacher/Teacher, Parent)

ALTER TABLE public.media_assets
    ADD COLUMN IF NOT EXISTS branch_id UUID REFERENCES public.branches(id) ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS classroom_id UUID REFERENCES public.classrooms(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_media_assets_org ON public.media_assets(organization_id);
CREATE INDEX IF NOT EXISTS idx_media_assets_branch ON public.media_assets(branch_id);
CREATE INDEX IF NOT EXISTS idx_media_assets_classroom ON public.media_assets(classroom_id);

-- Backfill classroom_id and branch_id from children + classrooms for existing child-linked media
UPDATE public.media_assets m
SET
    classroom_id = COALESCE(m.classroom_id, ch.classroom_id),
    branch_id = COALESCE(m.branch_id, cl.branch_id),
    organization_id = COALESCE(m.organization_id, b.organization_id)
FROM public.children ch
LEFT JOIN public.classrooms cl ON cl.id = ch.classroom_id
LEFT JOIN public.branches b ON b.id = cl.branch_id
WHERE m.child_id = ch.id
  AND (m.classroom_id IS NULL OR m.branch_id IS NULL OR m.organization_id IS NULL);

-- Update fn_get_media_asset_by_id to return branch_id and classroom_id
DROP FUNCTION IF EXISTS public.fn_get_media_asset_by_id(uuid, uuid);

CREATE OR REPLACE FUNCTION public.fn_get_media_asset_by_id(p_tenant_id uuid, p_id uuid)
RETURNS TABLE(
    id uuid,
    tenant_id uuid,
    organization_id uuid,
    branch_id uuid,
    classroom_id uuid,
    child_id uuid,
    entity_type character varying,
    entity_id uuid,
    display_container character varying,
    display_key character varying,
    display_content_type character varying,
    display_size_bytes bigint,
    display_width integer,
    display_height integer,
    thumbnail_container character varying,
    thumbnail_key character varying,
    thumbnail_content_type character varying,
    thumbnail_size_bytes bigint,
    thumbnail_width integer,
    thumbnail_height integer,
    content_hash character varying,
    created_by uuid,
    created_at timestamp with time zone,
    updated_at timestamp with time zone
)
LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT
        m.id, m.tenant_id, m.organization_id, m.branch_id, m.classroom_id, m.child_id,
        m.entity_type, m.entity_id,
        m.display_container, m.display_key, m.display_content_type, m.display_size_bytes, m.display_width, m.display_height,
        m.thumbnail_container, m.thumbnail_key, m.thumbnail_content_type, m.thumbnail_size_bytes, m.thumbnail_width, m.thumbnail_height,
        m.content_hash, m.created_by, m.created_at, m.updated_at
    FROM public.media_assets m
    WHERE m.id = p_id AND m.tenant_id = p_tenant_id;
END;
$function$;

-- Update sp_create_media_asset to accept optional p_branch_id and p_classroom_id
DROP PROCEDURE IF EXISTS public.sp_create_media_asset(
    uuid, uuid, uuid, uuid, character varying, uuid,
    character varying, character varying, character varying, bigint, integer, integer,
    character varying, character varying, character varying, bigint, integer, integer,
    character varying, uuid, timestamp with time zone, timestamp with time zone
);

CREATE OR REPLACE PROCEDURE public.sp_create_media_asset(
    IN p_id uuid,
    IN p_tenant_id uuid,
    IN p_organization_id uuid,
    IN p_child_id uuid,
    IN p_entity_type character varying,
    IN p_entity_id uuid,
    IN p_display_container character varying,
    IN p_display_key character varying,
    IN p_display_content_type character varying,
    IN p_display_size_bytes bigint,
    IN p_display_width integer,
    IN p_display_height integer,
    IN p_thumbnail_container character varying,
    IN p_thumbnail_key character varying,
    IN p_thumbnail_content_type character varying,
    IN p_thumbnail_size_bytes bigint,
    IN p_thumbnail_width integer,
    IN p_thumbnail_height integer,
    IN p_content_hash character varying,
    IN p_created_by uuid,
    IN p_created_at timestamp with time zone,
    IN p_updated_at timestamp with time zone,
    IN p_branch_id uuid DEFAULT NULL,
    IN p_classroom_id uuid DEFAULT NULL
)
LANGUAGE plpgsql
AS $procedure$
BEGIN
    INSERT INTO public.media_assets (
        id, tenant_id, organization_id, branch_id, classroom_id, child_id, entity_type, entity_id,
        display_container, display_key, display_content_type, display_size_bytes, display_width, display_height,
        thumbnail_container, thumbnail_key, thumbnail_content_type, thumbnail_size_bytes, thumbnail_width, thumbnail_height,
        content_hash, created_by, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id, p_entity_type, p_entity_id,
        p_display_container, p_display_key, p_display_content_type, p_display_size_bytes, p_display_width, p_display_height,
        p_thumbnail_container, p_thumbnail_key, p_thumbnail_content_type, p_thumbnail_size_bytes, p_thumbnail_width, p_thumbnail_height,
        p_content_hash, p_created_by, p_created_at, p_updated_at
    );
END;
$procedure$;
