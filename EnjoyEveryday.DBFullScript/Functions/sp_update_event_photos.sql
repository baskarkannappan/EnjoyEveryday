-- Function: public.sp_update_event_photos
-- Backup generated on 2026-10-25 20:34:09

CREATE OR REPLACE PROCEDURE public.sp_update_event_photos(IN p_id uuid, IN p_tenant_id uuid, IN p_photo_urls text[], IN p_updated_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE events      SET photo_urls = p_photo_urls, updated_at = p_updated_at     WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;
