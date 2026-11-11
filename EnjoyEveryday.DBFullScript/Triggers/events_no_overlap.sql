-- Trigger: events_no_overlap
-- Backup generated on 2026-11-11 09:06:13

CREATE TRIGGER events_no_overlap BEFORE UPDATE ON public.events FOR EACH ROW EXECUTE FUNCTION trg_events_no_overlap();
