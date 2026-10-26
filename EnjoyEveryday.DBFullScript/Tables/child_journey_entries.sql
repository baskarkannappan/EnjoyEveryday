--
-- PostgreSQL database dump
--

\restrict 8KPMZCzJ0cjcPI7PsoHVUKG0fhfFDhxFjuJRbeoNsrqbEiKkgelJQ4rON0C5w9j

-- Dumped from database version 15.19
-- Dumped by pg_dump version 15.19

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: child_journey_entries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_entries (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    enrollment_id uuid,
    classroom_id uuid,
    teacher_id uuid NOT NULL,
    experience_id uuid,
    experience_schedule_id uuid,
    little_moment_id uuid,
    milestone_id uuid,
    journey_type character varying(100) NOT NULL,
    title character varying(255),
    observation text NOT NULL,
    child_voice text,
    teacher_reflection text,
    observation_date timestamp with time zone NOT NULL,
    participation_type character varying(100),
    is_milestone boolean DEFAULT false NOT NULL,
    is_parent_visible boolean DEFAULT false NOT NULL,
    parent_visibility_status character varying(50),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by uuid,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_by uuid,
    version integer DEFAULT 1 NOT NULL,
    experience_session_id uuid,
    experience_participation_id uuid,
    event_id uuid
);


--
-- Name: child_journey_entries child_journey_entries_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_pkey PRIMARY KEY (id);


--
-- Name: idx_child_journey_participation; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_journey_participation ON public.child_journey_entries USING btree (experience_participation_id);


--
-- Name: idx_child_journey_session; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_journey_session ON public.child_journey_entries USING btree (experience_session_id);


--
-- Name: idx_journey_entries_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_child ON public.child_journey_entries USING btree (child_id);


--
-- Name: idx_journey_entries_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_classroom ON public.child_journey_entries USING btree (classroom_id);


--
-- Name: idx_journey_entries_portfolio; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_portfolio ON public.child_journey_entries USING btree (tenant_id, child_id, is_parent_visible, observation_date DESC);


--
-- Name: idx_journey_entries_teacher; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_teacher ON public.child_journey_entries USING btree (teacher_id);


--
-- Name: idx_journey_entries_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_tenant ON public.child_journey_entries USING btree (tenant_id);


--
-- Name: child_journey_entries child_journey_entries_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_journey_entries child_journey_entries_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_experience_participation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_experience_participation_id_fkey FOREIGN KEY (experience_participation_id) REFERENCES public.experience_participations(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_experience_session_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_experience_session_id_fkey FOREIGN KEY (experience_session_id) REFERENCES public.experience_sessions(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_teacher_id_fkey FOREIGN KEY (teacher_id) REFERENCES public.teachers(id);


--
-- Name: child_journey_entries child_journey_entries_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 8KPMZCzJ0cjcPI7PsoHVUKG0fhfFDhxFjuJRbeoNsrqbEiKkgelJQ4rON0C5w9j

