--
-- PostgreSQL database dump
--

\restrict iFVBjFwpBBCgqsDNV0jNXBwyw2RVG0hFrJcx5ZLE5D10qqnN7BeZQjXQPAuubAr

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
-- Name: weekly_planning_draft_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.weekly_planning_draft_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    request_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    scheduled_date date NOT NULL,
    day_of_week character varying(20) NOT NULL,
    time_of_day character varying(50) DEFAULT 'Morning'::character varying NOT NULL,
    experience_id uuid NOT NULL,
    experience_title character varying(255) NOT NULL,
    learning_goal text,
    group_format character varying(50) DEFAULT 'Small Group'::character varying,
    estimated_duration_minutes integer DEFAULT 25,
    required_materials text,
    rationale text,
    validation_status character varying(50) DEFAULT 'Valid'::character varying NOT NULL,
    validation_notes text,
    resulting_schedule_id uuid,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: weekly_planning_draft_items weekly_planning_draft_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_items
    ADD CONSTRAINT weekly_planning_draft_items_pkey PRIMARY KEY (id);


--
-- Name: idx_weekly_planning_items_request; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_weekly_planning_items_request ON public.weekly_planning_draft_items USING btree (request_id, scheduled_date);


--
-- Name: weekly_planning_draft_items weekly_planning_draft_items_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_items
    ADD CONSTRAINT weekly_planning_draft_items_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE RESTRICT;


--
-- Name: weekly_planning_draft_items weekly_planning_draft_items_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_items
    ADD CONSTRAINT weekly_planning_draft_items_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.weekly_planning_draft_requests(id) ON DELETE CASCADE;


--
-- Name: weekly_planning_draft_items weekly_planning_draft_items_resulting_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_items
    ADD CONSTRAINT weekly_planning_draft_items_resulting_schedule_id_fkey FOREIGN KEY (resulting_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- PostgreSQL database dump complete
--

\unrestrict iFVBjFwpBBCgqsDNV0jNXBwyw2RVG0hFrJcx5ZLE5D10qqnN7BeZQjXQPAuubAr

