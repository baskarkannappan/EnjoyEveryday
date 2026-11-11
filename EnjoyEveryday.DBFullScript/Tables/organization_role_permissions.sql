--
-- PostgreSQL database dump
--

\restrict z4XP8Lir1r56rMqbe38vax2PRSeL8kb3er41dpNjRESiC6Z1Z7SsKaDn8WOkXmV

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
-- Name: organization_role_permissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organization_role_permissions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    role_name character varying(100) NOT NULL,
    feature_key character varying(100) NOT NULL,
    is_allowed boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: organization_role_permissions organization_role_permissions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_role_permissions
    ADD CONSTRAINT organization_role_permissions_pkey PRIMARY KEY (id);


--
-- Name: organization_role_permissions uk_org_role_feature; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_role_permissions
    ADD CONSTRAINT uk_org_role_feature UNIQUE (tenant_id, organization_id, role_name, feature_key);


--
-- Name: idx_org_role_perms_lookup; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_role_perms_lookup ON public.organization_role_permissions USING btree (tenant_id, organization_id, role_name);


--
-- Name: organization_role_permissions organization_role_permissions_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_role_permissions
    ADD CONSTRAINT organization_role_permissions_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: organization_role_permissions organization_role_permissions_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_role_permissions
    ADD CONSTRAINT organization_role_permissions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict z4XP8Lir1r56rMqbe38vax2PRSeL8kb3er41dpNjRESiC6Z1Z7SsKaDn8WOkXmV

