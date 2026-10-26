--
-- PostgreSQL database dump
--

\restrict kFLeUNefPwOhu81eV89PRwrmh2QEGYqzRsXY87m9MhVX3tbykg5QCUKgSImnt0V

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
-- Name: organization_intelligence_insights; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organization_intelligence_insights (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid,
    classroom_id uuid,
    scope_type character varying(50) NOT NULL,
    reporting_period character varying(50) NOT NULL,
    category character varying(50) NOT NULL,
    title character varying(255) NOT NULL,
    observation text NOT NULL,
    supporting_metrics_json jsonb,
    possible_explanation text,
    suggested_next_step text,
    evidence_strength character varying(50) DEFAULT 'Strong'::character varying NOT NULL,
    data_limitations text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    source_type character varying(50) DEFAULT 'DeterministicRule'::character varying NOT NULL,
    is_ai_generated boolean DEFAULT false NOT NULL
);


--
-- Name: organization_intelligence_insights organization_intelligence_insights_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_pkey PRIMARY KEY (id);


--
-- Name: idx_org_intel_category; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_intel_category ON public.organization_intelligence_insights USING btree (tenant_id, category);


--
-- Name: idx_org_intel_created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_intel_created ON public.organization_intelligence_insights USING btree (tenant_id, created_at DESC);


--
-- Name: idx_org_intel_tenant_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_intel_tenant_org ON public.organization_intelligence_insights USING btree (tenant_id, organization_id);


--
-- Name: organization_intelligence_insights organization_intelligence_insights_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id) ON DELETE CASCADE;


--
-- Name: organization_intelligence_insights organization_intelligence_insights_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: organization_intelligence_insights organization_intelligence_insights_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: organization_intelligence_insights organization_intelligence_insights_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict kFLeUNefPwOhu81eV89PRwrmh2QEGYqzRsXY87m9MhVX3tbykg5QCUKgSImnt0V

