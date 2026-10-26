--
-- PostgreSQL database dump
--

\restrict kij0S3zPc2QCDHeC3GpJ00oKhD5fcWEXz73mHzmNohM4cvfbPmkfq0cZfAMaTea

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
-- Name: branch_operational_goals; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.branch_operational_goals (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid,
    branch_id uuid,
    title character varying(200) NOT NULL,
    category character varying(50) DEFAULT 'Enrollment'::character varying NOT NULL,
    target_metric character varying(100),
    current_value character varying(50),
    target_value character varying(50),
    status character varying(50) DEFAULT 'InProgress'::character varying NOT NULL,
    target_date date,
    owner_name character varying(100),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: branch_operational_goals branch_operational_goals_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_operational_goals
    ADD CONSTRAINT branch_operational_goals_pkey PRIMARY KEY (id);


--
-- Name: idx_branch_goals_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_branch_goals_branch ON public.branch_operational_goals USING btree (branch_id);


--
-- Name: idx_branch_goals_tenant_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_branch_goals_tenant_org ON public.branch_operational_goals USING btree (tenant_id, organization_id);


--
-- Name: branch_operational_goals branch_operational_goals_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_operational_goals
    ADD CONSTRAINT branch_operational_goals_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: branch_operational_goals branch_operational_goals_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_operational_goals
    ADD CONSTRAINT branch_operational_goals_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: branch_operational_goals branch_operational_goals_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_operational_goals
    ADD CONSTRAINT branch_operational_goals_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict kij0S3zPc2QCDHeC3GpJ00oKhD5fcWEXz73mHzmNohM4cvfbPmkfq0cZfAMaTea

