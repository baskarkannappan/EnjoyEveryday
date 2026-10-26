--
-- PostgreSQL database dump
--

\restrict nyPsmzScffQv8d9Nx3h1zYg8jUJkJwC2yUb7AvNUaia4H4WdXmCAP884VEhBf5A

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
-- Name: experience_feedback; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_feedback (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    experience_schedule_id uuid,
    teacher_id uuid NOT NULL,
    rating character varying(50) NOT NULL,
    notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    stars integer DEFAULT 5 NOT NULL,
    experience_id uuid
);


--
-- Name: experience_feedback experience_feedback_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_feedback
    ADD CONSTRAINT experience_feedback_pkey PRIMARY KEY (id);


--
-- Name: idx_experience_feedback_schedule; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_feedback_schedule ON public.experience_feedback USING btree (experience_schedule_id);


--
-- Name: idx_experience_feedback_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_feedback_tenant ON public.experience_feedback USING btree (tenant_id);


--
-- Name: experience_feedback experience_feedback_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_feedback
    ADD CONSTRAINT experience_feedback_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict nyPsmzScffQv8d9Nx3h1zYg8jUJkJwC2yUb7AvNUaia4H4WdXmCAP884VEhBf5A

