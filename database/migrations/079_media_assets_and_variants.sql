-- Migration 079: Create media_assets table and lookup procedures
-- Supporting secure private multi-variant image storage without third-party dependencies

CREATE TABLE IF NOT EXISTS public.media_assets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES public.tenants(id) ON DELETE CASCADE,
    organization_id UUID REFERENCES public.organizations(id) ON DELETE SET NULL,
    child_id UUID REFERENCES public.children(id) ON DELETE CASCADE,
    entity_type VARCHAR(50) NOT NULL, -- 'ChildJourney', 'ChildProfile', 'Event', 'TeacherProfile', etc.
    entity_id UUID,
    
    display_container VARCHAR(100) NOT NULL,
    display_key VARCHAR(255) NOT NULL,
    display_content_type VARCHAR(100) NOT NULL,
    display_size_bytes BIGINT NOT NULL,
    display_width INT NOT NULL,
    display_height INT NOT NULL,

    thumbnail_container VARCHAR(100) NOT NULL,
    thumbnail_key VARCHAR(255) NOT NULL,
    thumbnail_content_type VARCHAR(100) NOT NULL,
    thumbnail_size_bytes BIGINT NOT NULL,
    thumbnail_width INT NOT NULL,
    thumbnail_height INT NOT NULL,

    content_hash VARCHAR(128) NOT NULL,
    created_by UUID,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_media_assets_tenant ON public.media_assets(tenant_id);
CREATE INDEX IF NOT EXISTS idx_media_assets_child ON public.media_assets(child_id);
CREATE INDEX IF NOT EXISTS idx_media_assets_entity ON public.media_assets(entity_type, entity_id);

-- Procedure to create media asset
CREATE OR REPLACE PROCEDURE public.sp_create_media_asset(
    IN p_id UUID,
    IN p_tenant_id UUID,
    IN p_organization_id UUID,
    IN p_child_id UUID,
    IN p_entity_type VARCHAR,
    IN p_entity_id UUID,
    IN p_display_container VARCHAR,
    IN p_display_key VARCHAR,
    IN p_display_content_type VARCHAR,
    IN p_display_size_bytes BIGINT,
    IN p_display_width INT,
    IN p_display_height INT,
    IN p_thumbnail_container VARCHAR,
    IN p_thumbnail_key VARCHAR,
    IN p_thumbnail_content_type VARCHAR,
    IN p_thumbnail_size_bytes BIGINT,
    IN p_thumbnail_width INT,
    IN p_thumbnail_height INT,
    IN p_content_hash VARCHAR,
    IN p_created_by UUID,
    IN p_created_at TIMESTAMPTZ,
    IN p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.media_assets (
        id, tenant_id, organization_id, child_id, entity_type, entity_id,
        display_container, display_key, display_content_type, display_size_bytes, display_width, display_height,
        thumbnail_container, thumbnail_key, thumbnail_content_type, thumbnail_size_bytes, thumbnail_width, thumbnail_height,
        content_hash, created_by, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_child_id, p_entity_type, p_entity_id,
        p_display_container, p_display_key, p_display_content_type, p_display_size_bytes, p_display_width, p_display_height,
        p_thumbnail_container, p_thumbnail_key, p_thumbnail_content_type, p_thumbnail_size_bytes, p_thumbnail_width, p_thumbnail_height,
        p_content_hash, p_created_by, p_created_at, p_updated_at
    );
END;
$$;

-- Function to get media asset by ID
CREATE OR REPLACE FUNCTION public.fn_get_media_asset_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    child_id UUID,
    entity_type VARCHAR,
    entity_id UUID,
    display_container VARCHAR,
    display_key VARCHAR,
    display_content_type VARCHAR,
    display_size_bytes BIGINT,
    display_width INT,
    display_height INT,
    thumbnail_container VARCHAR,
    thumbnail_key VARCHAR,
    thumbnail_content_type VARCHAR,
    thumbnail_size_bytes BIGINT,
    thumbnail_width INT,
    thumbnail_height INT,
    content_hash VARCHAR,
    created_by UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        m.id, m.tenant_id, m.organization_id, m.child_id, m.entity_type, m.entity_id,
        m.display_container, m.display_key, m.display_content_type, m.display_size_bytes, m.display_width, m.display_height,
        m.thumbnail_container, m.thumbnail_key, m.thumbnail_content_type, m.thumbnail_size_bytes, m.thumbnail_width, m.thumbnail_height,
        m.content_hash, m.created_by, m.created_at, m.updated_at
    FROM public.media_assets m
    WHERE m.id = p_id AND m.tenant_id = p_tenant_id;
END;
$$;

-- Function to delete media asset
CREATE OR REPLACE PROCEDURE public.sp_delete_media_asset(
    IN p_tenant_id UUID,
    IN p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.media_assets
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;
