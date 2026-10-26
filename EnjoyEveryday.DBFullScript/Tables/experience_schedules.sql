--
-- PostgreSQL database dump
--

\restrict RkL90349NdXI7inuI3837KRksOYR3YelaZf15uOLfSMCwKaHYpOXbE2pNNTnEUR

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
-- Name: experience_schedules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_schedules (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    scheduled_date date NOT NULL,
    time_of_day character varying(50) NOT NULL,
    status character varying(50) DEFAULT 'Scheduled'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    primary_teacher_id uuid,
    planned_start_time time without time zone,
    planned_end_time time without time zone,
    preparation_notes text
);


--
-- Name: experience_schedules experience_schedules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT experience_schedules_pkey PRIMARY KEY (id);


--
-- Name: idx_es_tenant_date_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_es_tenant_date_classroom ON public.experience_schedules USING btree (tenant_id, scheduled_date, classroom_id);


--
-- Name: idx_experience_schedules_classroom_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_schedules_classroom_date ON public.experience_schedules USING btree (classroom_id, scheduled_date);


--
-- Name: idx_experience_schedules_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_schedules_tenant ON public.experience_schedules USING btree (tenant_id);


--
-- Name: experience_schedules experience_schedules_primary_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT experience_schedules_primary_teacher_id_fkey FOREIGN KEY (primary_teacher_id) REFERENCES public.teachers(id) ON DELETE SET NULL;


--
-- Name: experience_schedules fk_experience_schedules_classroom; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT fk_experience_schedules_classroom FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: experience_schedules fk_experience_schedules_experience; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT fk_experience_schedules_experience FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: experience_schedules fk_experience_schedules_tenant; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT fk_experience_schedules_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict RkL90349NdXI7inuI3837KRksOYR3YelaZf15uOLfSMCwKaHYpOXbE2pNNTnEUR

