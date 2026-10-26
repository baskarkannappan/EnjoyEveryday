--
-- PostgreSQL database dump
--

\restrict y1fdV2AMyEhZOeNaPSuc1ntVO19ENrgjoS4ehubNUWl0UY1ZlNq5ePwwf9HBNx6

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
-- Name: child_care_events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_care_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid,
    child_id uuid NOT NULL,
    event_type character varying(50) NOT NULL,
    event_time timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    diaper_condition character varying(50),
    skin_observation character varying(200),
    meal_type character varying(50),
    food_offered character varying(250),
    amount_consumed character varying(50),
    fluid_oz numeric(4,1),
    rest_status character varying(50),
    toileting_activity character varying(50),
    hygiene_type character varying(50),
    comfort_mood character varying(50),
    notes text,
    parent_visible boolean DEFAULT true NOT NULL,
    recorded_by_user_id uuid NOT NULL,
    recorded_by_name character varying(150),
    correction_reason text,
    is_corrected boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_care_events child_care_events_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_pkey PRIMARY KEY (id);


--
-- Name: idx_cce_child_time; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cce_child_time ON public.child_care_events USING btree (child_id, event_time DESC);


--
-- Name: idx_cce_classroom_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cce_classroom_date ON public.child_care_events USING btree (classroom_id, event_time);


--
-- Name: idx_cce_tenant_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cce_tenant_org ON public.child_care_events USING btree (tenant_id, organization_id);


--
-- Name: child_care_events child_care_events_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_care_events child_care_events_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_care_events child_care_events_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_care_events child_care_events_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_care_events child_care_events_recorded_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_recorded_by_user_id_fkey FOREIGN KEY (recorded_by_user_id) REFERENCES public.users(id);


--
-- Name: child_care_events child_care_events_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict y1fdV2AMyEhZOeNaPSuc1ntVO19ENrgjoS4ehubNUWl0UY1ZlNq5ePwwf9HBNx6

