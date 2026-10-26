--
-- PostgreSQL database dump
--

\restrict z2xEB77mGXEmxtjl4FjP9mRI3TLZn1ZRTjvTrDDayebLmQkXnBeuam0yrJFrkeB

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
-- Name: weekly_planning_draft_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.weekly_planning_draft_requests (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    requested_by_user_id uuid NOT NULL,
    week_start_date date NOT NULL,
    week_end_date date NOT NULL,
    focus_areas character varying(255),
    indoor_outdoor_preference character varying(50) DEFAULT 'Balanced'::character varying,
    materials_notes text,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: weekly_planning_draft_requests weekly_planning_draft_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_requests
    ADD CONSTRAINT weekly_planning_draft_requests_pkey PRIMARY KEY (id);


--
-- Name: idx_weekly_planning_requests_tenant_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_weekly_planning_requests_tenant_classroom ON public.weekly_planning_draft_requests USING btree (tenant_id, classroom_id, week_start_date DESC);


--
-- Name: weekly_planning_draft_requests weekly_planning_draft_requests_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_requests
    ADD CONSTRAINT weekly_planning_draft_requests_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict z2xEB77mGXEmxtjl4FjP9mRI3TLZn1ZRTjvTrDDayebLmQkXnBeuam0yrJFrkeB

