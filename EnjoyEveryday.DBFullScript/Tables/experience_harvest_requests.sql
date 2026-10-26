--
-- PostgreSQL database dump
--

\restrict 8VvOvMzcnycD2WW7hDOgb9j9PULMkQvliNeJttjkmta52Ofb0rQoiAC56xO828e

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
-- Name: experience_harvest_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_harvest_requests (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    source_experience_id uuid NOT NULL,
    source_version_number integer DEFAULT 1 NOT NULL,
    license_id uuid,
    submitted_by_user_id uuid NOT NULL,
    submitted_at timestamp with time zone DEFAULT now() NOT NULL,
    status character varying(50) DEFAULT 'Submitted'::character varying NOT NULL,
    review_notes text,
    reviewed_by_user_id uuid,
    reviewed_at timestamp with time zone,
    safety_suitability_verified boolean DEFAULT false NOT NULL,
    learning_objectives_verified boolean DEFAULT false NOT NULL,
    instructions_verified boolean DEFAULT false NOT NULL,
    materials_verified boolean DEFAULT false NOT NULL,
    media_rights_verified boolean DEFAULT false NOT NULL,
    commercial_rights_verified boolean DEFAULT false NOT NULL
);


--
-- Name: experience_harvest_requests experience_harvest_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_pkey PRIMARY KEY (id);


--
-- Name: idx_harvest_requests_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_harvest_requests_org ON public.experience_harvest_requests USING btree (organization_id);


--
-- Name: idx_harvest_requests_source; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_harvest_requests_source ON public.experience_harvest_requests USING btree (source_experience_id);


--
-- Name: idx_harvest_requests_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_harvest_requests_status ON public.experience_harvest_requests USING btree (status);


--
-- Name: experience_harvest_requests experience_harvest_requests_license_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_license_id_fkey FOREIGN KEY (license_id) REFERENCES public.experience_licenses(id) ON DELETE SET NULL;


--
-- Name: experience_harvest_requests experience_harvest_requests_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: experience_harvest_requests experience_harvest_requests_reviewed_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_reviewed_by_user_id_fkey FOREIGN KEY (reviewed_by_user_id) REFERENCES public.users(id);


--
-- Name: experience_harvest_requests experience_harvest_requests_source_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_source_experience_id_fkey FOREIGN KEY (source_experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: experience_harvest_requests experience_harvest_requests_submitted_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_submitted_by_user_id_fkey FOREIGN KEY (submitted_by_user_id) REFERENCES public.users(id);


--
-- Name: experience_harvest_requests experience_harvest_requests_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 8VvOvMzcnycD2WW7hDOgb9j9PULMkQvliNeJttjkmta52Ofb0rQoiAC56xO828e

