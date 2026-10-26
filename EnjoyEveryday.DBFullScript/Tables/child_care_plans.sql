--
-- PostgreSQL database dump
--

\restrict 5EykVGCWHVtn79sqrFNq7nBWoTYemqfdg2ENQ3W98VjJDX6A9swWaU8JTWQ7cY9

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
-- Name: child_care_plans; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_care_plans (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    child_id uuid NOT NULL,
    version integer DEFAULT 1 NOT NULL,
    diaper_routine_guidance text,
    feeding_instructions text,
    bottle_schedule_guidance text,
    sleep_routine_guidance text,
    toileting_support_plan text,
    comfort_calming_preferences text,
    required_supplies_note text,
    dietary_allergies_summary text,
    effective_date date DEFAULT CURRENT_DATE NOT NULL,
    review_date date,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    created_by_user_id uuid NOT NULL,
    approved_by_user_id uuid,
    approved_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_care_plans child_care_plans_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_pkey PRIMARY KEY (id);


--
-- Name: idx_ccp_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ccp_child ON public.child_care_plans USING btree (child_id, status);


--
-- Name: child_care_plans child_care_plans_approved_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_approved_by_user_id_fkey FOREIGN KEY (approved_by_user_id) REFERENCES public.users(id);


--
-- Name: child_care_plans child_care_plans_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_care_plans child_care_plans_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_care_plans child_care_plans_created_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_created_by_user_id_fkey FOREIGN KEY (created_by_user_id) REFERENCES public.users(id);


--
-- Name: child_care_plans child_care_plans_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_care_plans child_care_plans_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 5EykVGCWHVtn79sqrFNq7nBWoTYemqfdg2ENQ3W98VjJDX6A9swWaU8JTWQ7cY9

