--
-- PostgreSQL database dump
--

\restrict ikYI4RayN0nOgHW8qiQvYTjmUGJVnJbs74Db65iyJLh22aVZIQReAr5NzIyeNMr

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
-- Name: experience_audit_events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_audit_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid,
    organization_id uuid,
    platform_experience_id uuid,
    action character varying(100) NOT NULL,
    performed_by_user_id uuid NOT NULL,
    details jsonb,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: experience_audit_events experience_audit_events_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_audit_events
    ADD CONSTRAINT experience_audit_events_pkey PRIMARY KEY (id);


--
-- Name: idx_exp_audit_action; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_audit_action ON public.experience_audit_events USING btree (action);


--
-- Name: idx_exp_audit_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_audit_org ON public.experience_audit_events USING btree (organization_id);


--
-- Name: experience_audit_events experience_audit_events_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_audit_events
    ADD CONSTRAINT experience_audit_events_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: experience_audit_events experience_audit_events_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_audit_events
    ADD CONSTRAINT experience_audit_events_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict ikYI4RayN0nOgHW8qiQvYTjmUGJVnJbs74Db65iyJLh22aVZIQReAr5NzIyeNMr

