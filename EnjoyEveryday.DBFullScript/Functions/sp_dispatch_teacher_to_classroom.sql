-- Function: public.sp_dispatch_teacher_to_classroom
-- Backup generated on 2026-11-11 09:05:51

CREATE OR REPLACE PROCEDURE public.sp_dispatch_teacher_to_classroom(IN p_tenant_id uuid, IN p_classroom_id uuid, IN p_user_id uuid)  LANGUAGE plpgsql AS $procedure$ DECLARE     v_canonical_tenant_id uuid; BEGIN     SELECT tenant_id INTO v_canonical_tenant_id FROM classrooms WHERE id = p_classroom_id;     IF v_canonical_tenant_id IS NULL THEN         v_canonical_tenant_id := p_tenant_id;     END IF;      INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)     VALUES (gen_random_uuid(), v_canonical_tenant_id, p_classroom_id, p_user_id, FALSE)     ON CONFLICT (classroom_id, user_id) DO UPDATE     SET tenant_id = v_canonical_tenant_id; END; $procedure$ ;
