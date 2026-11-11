-- Function: public.trg_event_classrooms_no_overlap
-- Backup generated on 2026-11-11 09:06:11

CREATE OR REPLACE FUNCTION public.trg_event_classrooms_no_overlap()  RETURNS trigger  LANGUAGE plpgsql AS $function$ DECLARE     e record; BEGIN     SELECT id, tenant_id, event_date, start_time, end_time, status INTO e FROM events WHERE id = NEW.event_id;     IF FOUND AND e.status <> 'Cancelled' THEN         PERFORM public.fn_assert_classroom_free(e.tenant_id, NEW.classroom_id, e.event_date, e.start_time, e.end_time, e.id);     END IF;     RETURN NEW; END; $function$ ;
