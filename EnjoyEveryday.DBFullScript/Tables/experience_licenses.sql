--
-- PostgreSQL database dump
--

\restrict x2FMSrhgNIcEcwPVpA8djO7w8sU7QhbUeIP3y9mGoZRj2zaI3kFeeQa1WgBLHqN

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
-- Name: experience_licenses; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_licenses (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    organization_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    license_type character varying(50) DEFAULT 'CommercialRoyalty'::character varying NOT NULL,
    grant_scope character varying(150) DEFAULT 'Curate, Reproduce, Distribute, Modify and Monetize'::character varying NOT NULL,
    granted_by_user_id uuid NOT NULL,
    granted_at timestamp with time zone DEFAULT now() NOT NULL,
    expires_at timestamp with time zone,
    terms_document_hash character varying(128),
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: experience_licenses experience_licenses_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_licenses
    ADD CONSTRAINT experience_licenses_pkey PRIMARY KEY (id);


--
-- Name: idx_experience_licenses_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_licenses_exp ON public.experience_licenses USING btree (experience_id);


--
-- Name: idx_experience_licenses_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_licenses_org ON public.experience_licenses USING btree (organization_id);


--
-- Name: experience_licenses experience_licenses_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_licenses
    ADD CONSTRAINT experience_licenses_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: experience_licenses experience_licenses_granted_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_licenses
    ADD CONSTRAINT experience_licenses_granted_by_user_id_fkey FOREIGN KEY (granted_by_user_id) REFERENCES public.users(id);


--
-- Name: experience_licenses experience_licenses_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_licenses
    ADD CONSTRAINT experience_licenses_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict x2FMSrhgNIcEcwPVpA8djO7w8sU7QhbUeIP3y9mGoZRj2zaI3kFeeQa1WgBLHqN

