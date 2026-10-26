--
-- PostgreSQL database dump
--

\restrict IdSeOaCGIg0rsJrtz4Wbi8F9dQD9R8jd9TxXK2pfQvEPRnZ87h7gcmcbPuvAhec

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
-- Name: organization_entitlements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organization_entitlements (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    platform_experience_id uuid NOT NULL,
    order_id uuid,
    acquired_version integer DEFAULT 1 NOT NULL,
    entitlement_type character varying(50) DEFAULT 'OneTimePurchase'::character varying NOT NULL,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    granted_at timestamp with time zone DEFAULT now() NOT NULL,
    expires_at timestamp with time zone,
    scope character varying(50) DEFAULT 'OrganizationWide'::character varying NOT NULL
);


--
-- Name: organization_entitlements organization_entitlements_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_pkey PRIMARY KEY (id);


--
-- Name: organization_entitlements uq_org_entitlement; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT uq_org_entitlement UNIQUE (organization_id, platform_experience_id);


--
-- Name: idx_org_entitlements_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_entitlements_exp ON public.organization_entitlements USING btree (platform_experience_id);


--
-- Name: idx_org_entitlements_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_entitlements_org ON public.organization_entitlements USING btree (organization_id);


--
-- Name: idx_org_entitlements_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_entitlements_status ON public.organization_entitlements USING btree (status);


--
-- Name: organization_entitlements organization_entitlements_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.marketplace_orders(id) ON DELETE SET NULL;


--
-- Name: organization_entitlements organization_entitlements_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: organization_entitlements organization_entitlements_platform_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_platform_experience_id_fkey FOREIGN KEY (platform_experience_id) REFERENCES public.platform_experiences(id) ON DELETE CASCADE;


--
-- Name: organization_entitlements organization_entitlements_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict IdSeOaCGIg0rsJrtz4Wbi8F9dQD9R8jd9TxXK2pfQvEPRnZ87h7gcmcbPuvAhec

