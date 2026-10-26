--
-- PostgreSQL database dump
--

\restrict tNRzGTdNbT5ZbEo1Hsm9fi5PLaSXI4wRBaAKpavHDXMMsrQkFbLS2EWi61VYuuW

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
-- Name: experiences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experiences (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    title character varying(200) NOT NULL,
    description text,
    status character varying(50) DEFAULT 'Idea'::character varying NOT NULL,
    dna_payload jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_by_user_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    organization_id uuid
);


--
-- Name: experiences experiences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experiences
    ADD CONSTRAINT experiences_pkey PRIMARY KEY (id);


--
-- Name: idx_experiences_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experiences_org ON public.experiences USING btree (organization_id);


--
-- Name: idx_experiences_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experiences_status ON public.experiences USING btree (status);


--
-- Name: idx_experiences_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experiences_tenant ON public.experiences USING btree (tenant_id);


--
-- Name: experiences experiences_created_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experiences
    ADD CONSTRAINT experiences_created_by_user_id_fkey FOREIGN KEY (created_by_user_id) REFERENCES public.users(id);


--
-- Name: experiences experiences_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experiences
    ADD CONSTRAINT experiences_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: experiences experiences_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experiences
    ADD CONSTRAINT experiences_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict tNRzGTdNbT5ZbEo1Hsm9fi5PLaSXI4wRBaAKpavHDXMMsrQkFbLS2EWi61VYuuW

