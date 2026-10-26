--
-- PostgreSQL database dump
--

\restrict XgffPUfRawMDCRRcwMRRaV12thoBQecxN2L2oxCnbOaSgqt6JGIaQ2zMguVcSaf

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
-- Name: parent_communications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.parent_communications (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid,
    child_id uuid NOT NULL,
    recipient_person_id uuid,
    recipient_user_id uuid,
    recipient_email character varying(255),
    recipient_name character varying(255),
    recipient_relationship character varying(100),
    communication_type character varying(50) NOT NULL,
    reporting_date_start date NOT NULL,
    reporting_date_end date NOT NULL,
    title character varying(255) NOT NULL,
    content_draft text NOT NULL,
    content_final text NOT NULL,
    family_conversation_starter text,
    evidence_summary_json jsonb,
    consent_verified boolean DEFAULT true NOT NULL,
    consent_notes character varying(500),
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    teacher_id uuid NOT NULL,
    teacher_name character varying(255),
    approved_by_user_id uuid,
    approved_at timestamp with time zone,
    sent_at timestamp with time zone,
    delivery_channel character varying(50) DEFAULT 'PortalAndEmail'::character varying NOT NULL,
    delivery_status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    delivery_error text,
    idempotency_key character varying(100),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: parent_communications parent_communications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_pkey PRIMARY KEY (id);


--
-- Name: idx_parent_comms_recipient; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_parent_comms_recipient ON public.parent_communications USING btree (recipient_user_id);


--
-- Name: idx_parent_comms_tenant_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_parent_comms_tenant_child ON public.parent_communications USING btree (tenant_id, child_id);


--
-- Name: idx_parent_comms_tenant_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_parent_comms_tenant_status ON public.parent_communications USING btree (tenant_id, status);


--
-- Name: parent_communications parent_communications_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: parent_communications parent_communications_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: parent_communications parent_communications_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: parent_communications parent_communications_recipient_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_recipient_person_id_fkey FOREIGN KEY (recipient_person_id) REFERENCES public.persons(id);


--
-- Name: parent_communications parent_communications_recipient_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_recipient_user_id_fkey FOREIGN KEY (recipient_user_id) REFERENCES public.users(id);


--
-- Name: parent_communications parent_communications_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict XgffPUfRawMDCRRcwMRRaV12thoBQecxN2L2oxCnbOaSgqt6JGIaQ2zMguVcSaf

