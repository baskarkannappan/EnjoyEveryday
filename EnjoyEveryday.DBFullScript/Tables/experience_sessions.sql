--
-- PostgreSQL database dump
--

\restrict SBL6S3qyMa07Ucar77mjkSohXWdfcZtIsVEdmxVDK4YedWi1xD9X95zGiTIosta

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
-- Name: experience_sessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_sessions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    experience_schedule_id uuid,
    classroom_id uuid NOT NULL,
    session_date date NOT NULL,
    start_time time without time zone,
    end_time time without time zone,
    status character varying(50) DEFAULT 'Planned'::character varying NOT NULL,
    primary_teacher_id uuid,
    actual_notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: experience_sessions experience_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_pkey PRIMARY KEY (id);


--
-- Name: idx_experience_sessions_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_sessions_classroom ON public.experience_sessions USING btree (classroom_id);


--
-- Name: idx_experience_sessions_schedule; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_sessions_schedule ON public.experience_sessions USING btree (experience_schedule_id);


--
-- Name: idx_experience_sessions_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_sessions_tenant ON public.experience_sessions USING btree (tenant_id);


--
-- Name: experience_sessions experience_sessions_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: experience_sessions experience_sessions_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id);


--
-- Name: experience_sessions experience_sessions_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- Name: experience_sessions experience_sessions_primary_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_primary_teacher_id_fkey FOREIGN KEY (primary_teacher_id) REFERENCES public.users(id);


--
-- Name: experience_sessions experience_sessions_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict SBL6S3qyMa07Ucar77mjkSohXWdfcZtIsVEdmxVDK4YedWi1xD9X95zGiTIosta

