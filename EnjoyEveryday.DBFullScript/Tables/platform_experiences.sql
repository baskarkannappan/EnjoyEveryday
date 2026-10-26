--
-- PostgreSQL database dump
--

\restrict hI00zlcpoAuU7h9TBWFk5GHvvCUAsxsd95vAbofWAtuEFbNyfiQUKEZiVbv9BKd

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
-- Name: platform_experiences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.platform_experiences (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    title character varying(200) NOT NULL,
    description text NOT NULL,
    category character varying(100) DEFAULT 'General'::character varying NOT NULL,
    age_range_min_months integer,
    age_range_max_months integer,
    duration_minutes integer,
    learning_objectives jsonb,
    required_materials jsonb,
    instructions text,
    safety_notes text,
    dna_payload text NOT NULL,
    pricing_model character varying(50) DEFAULT 'IncludedInSubscription'::character varying NOT NULL,
    price_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    publication_status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    ownership_type character varying(50) DEFAULT 'CuratedLicensed'::character varying NOT NULL,
    current_version integer DEFAULT 1 NOT NULL,
    license_id uuid,
    source_harvest_request_id uuid,
    source_experience_id uuid,
    source_organization_id uuid,
    creator_attribution character varying(200),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: platform_experiences platform_experiences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experiences
    ADD CONSTRAINT platform_experiences_pkey PRIMARY KEY (id);


--
-- Name: idx_platform_exp_cat; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_platform_exp_cat ON public.platform_experiences USING btree (category);


--
-- Name: idx_platform_exp_pricing; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_platform_exp_pricing ON public.platform_experiences USING btree (pricing_model);


--
-- Name: idx_platform_exp_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_platform_exp_status ON public.platform_experiences USING btree (publication_status);


--
-- Name: platform_experiences platform_experiences_license_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experiences
    ADD CONSTRAINT platform_experiences_license_id_fkey FOREIGN KEY (license_id) REFERENCES public.experience_licenses(id) ON DELETE SET NULL;


--
-- Name: platform_experiences platform_experiences_source_harvest_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experiences
    ADD CONSTRAINT platform_experiences_source_harvest_request_id_fkey FOREIGN KEY (source_harvest_request_id) REFERENCES public.experience_harvest_requests(id) ON DELETE SET NULL;


--
-- PostgreSQL database dump complete
--

\unrestrict hI00zlcpoAuU7h9TBWFk5GHvvCUAsxsd95vAbofWAtuEFbNyfiQUKEZiVbv9BKd

