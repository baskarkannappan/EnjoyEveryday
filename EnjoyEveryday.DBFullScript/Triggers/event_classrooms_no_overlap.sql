-- Trigger: event_classrooms_no_overlap
-- Backup generated on 2026-11-11 09:06:13

CREATE TRIGGER event_classrooms_no_overlap BEFORE INSERT OR UPDATE ON public.event_classrooms FOR EACH ROW EXECUTE FUNCTION trg_event_classrooms_no_overlap();
