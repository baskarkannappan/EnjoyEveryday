--
-- PostgreSQL database dump
--

\restrict 3BMCsMhlGXi6Q2c7kngPdIBifmCKkUROFVVR8pCTAVviYT0qea6M4L46drRTJ4X

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
-- Name: consent_audit_events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.consent_audit_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid,
    consent_document_id uuid NOT NULL,
    actor_type character varying(50) NOT NULL,
    actor_user_id uuid,
    actor_name character varying(255),
    event_type character varying(100) NOT NULL,
    timestamp_utc timestamp with time zone DEFAULT now() NOT NULL,
    correlation_id character varying(100),
    safe_metadata text
);


--
-- Name: consent_audit_events consent_audit_events_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_pkey PRIMARY KEY (id);


--
-- Name: idx_consent_audit_doc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_audit_doc ON public.consent_audit_events USING btree (consent_document_id);


--
-- Name: idx_consent_audit_tenant_time; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_audit_tenant_time ON public.consent_audit_events USING btree (tenant_id, timestamp_utc DESC);


--
-- Name: consent_audit_events consent_audit_events_actor_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_actor_user_id_fkey FOREIGN KEY (actor_user_id) REFERENCES public.users(id);


--
-- Name: consent_audit_events consent_audit_events_consent_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_consent_document_id_fkey FOREIGN KEY (consent_document_id) REFERENCES public.child_consent_documents(id) ON DELETE CASCADE;


--
-- Name: consent_audit_events consent_audit_events_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: consent_audit_events consent_audit_events_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 3BMCsMhlGXi6Q2c7kngPdIBifmCKkUROFVVR8pCTAVviYT0qea6M4L46drRTJ4X

