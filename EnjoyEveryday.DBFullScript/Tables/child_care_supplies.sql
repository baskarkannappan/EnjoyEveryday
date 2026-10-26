--
-- PostgreSQL database dump
--

\restrict nzSONqgw8TReUvL5glCyY3oOrvUNwhvM3n4pQOFFNzKfZn36FDL9DxWUjNvEWBj

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
-- Name: child_care_supplies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_care_supplies (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid,
    child_id uuid NOT NULL,
    supply_type character varying(50) NOT NULL,
    status character varying(50) DEFAULT 'Adequate'::character varying NOT NULL,
    notes text,
    threshold_note character varying(150),
    last_reported_by_user_id uuid NOT NULL,
    last_reported_by_name character varying(150),
    parent_reminder_sent boolean DEFAULT false NOT NULL,
    last_reminder_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_care_supplies child_care_supplies_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_pkey PRIMARY KEY (id);


--
-- Name: idx_ccs_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ccs_child ON public.child_care_supplies USING btree (child_id, status);


--
-- Name: child_care_supplies child_care_supplies_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_care_supplies child_care_supplies_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_care_supplies child_care_supplies_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_care_supplies child_care_supplies_last_reported_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_last_reported_by_user_id_fkey FOREIGN KEY (last_reported_by_user_id) REFERENCES public.users(id);


--
-- Name: child_care_supplies child_care_supplies_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_care_supplies child_care_supplies_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict nzSONqgw8TReUvL5glCyY3oOrvUNwhvM3n4pQOFFNzKfZn36FDL9DxWUjNvEWBj

