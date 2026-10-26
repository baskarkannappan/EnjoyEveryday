--
-- PostgreSQL database dump
--

\restrict VkZIpzO1nUX4ATbtCnLAbpIYNobOtBU92Ae5bbEbB4COdndWIRkl449pgy99o4a

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
-- Name: consent_templates; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.consent_templates (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid,
    name character varying(255) NOT NULL,
    description text,
    document_type character varying(100) NOT NULL,
    version character varying(50) DEFAULT '1.0'::character varying NOT NULL,
    template_content text NOT NULL,
    required_for_enrollment boolean DEFAULT true NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_by uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: consent_templates consent_templates_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_templates
    ADD CONSTRAINT consent_templates_pkey PRIMARY KEY (id);


--
-- Name: idx_consent_templates_doc_type; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_templates_doc_type ON public.consent_templates USING btree (tenant_id, document_type);


--
-- Name: idx_consent_templates_tenant_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_templates_tenant_org ON public.consent_templates USING btree (tenant_id, organization_id);


--
-- Name: consent_templates consent_templates_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_templates
    ADD CONSTRAINT consent_templates_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(id);


--
-- Name: consent_templates consent_templates_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_templates
    ADD CONSTRAINT consent_templates_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: consent_templates consent_templates_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_templates
    ADD CONSTRAINT consent_templates_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict VkZIpzO1nUX4ATbtCnLAbpIYNobOtBU92Ae5bbEbB4COdndWIRkl449pgy99o4a

