--
-- PostgreSQL database dump
--

\restrict 2psju6zDqmSgk5GUDp9nTqgE1ga9XAHrUaYm2fJhspFEYkNlUaJgdcCo4y7N9gv

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
-- Name: organization_billing_settings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organization_billing_settings (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    mode character varying(20) DEFAULT 'Manual'::character varying NOT NULL,
    frequency character varying(20) DEFAULT 'Monthly'::character varying NOT NULL,
    effective_date timestamp with time zone DEFAULT now() NOT NULL,
    next_billing_date timestamp with time zone,
    auto_charge_enabled boolean DEFAULT false NOT NULL,
    notes text,
    updated_by character varying(200),
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: organization_billing_settings organization_billing_settings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_billing_settings
    ADD CONSTRAINT organization_billing_settings_pkey PRIMARY KEY (id);


--
-- Name: organization_billing_settings uk_org_billing_settings_org; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_billing_settings
    ADD CONSTRAINT uk_org_billing_settings_org UNIQUE (organization_id);


--
-- Name: idx_org_billing_settings_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_billing_settings_org ON public.organization_billing_settings USING btree (organization_id);


--
-- Name: organization_billing_settings organization_billing_settings_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_billing_settings
    ADD CONSTRAINT organization_billing_settings_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: organization_billing_settings organization_billing_settings_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_billing_settings
    ADD CONSTRAINT organization_billing_settings_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 2psju6zDqmSgk5GUDp9nTqgE1ga9XAHrUaYm2fJhspFEYkNlUaJgdcCo4y7N9gv

