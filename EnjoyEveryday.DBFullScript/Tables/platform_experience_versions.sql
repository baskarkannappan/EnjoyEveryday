--
-- PostgreSQL database dump
--

\restrict regifooKbzjSAr5AkCrSPyClhwe7HoHAbTlobxiHrnPLM9lMPuZ81fNe234f0M3

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
-- Name: platform_experience_versions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.platform_experience_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    platform_experience_id uuid NOT NULL,
    version_number integer NOT NULL,
    title character varying(200) NOT NULL,
    description text NOT NULL,
    dna_payload text NOT NULL,
    change_summary text,
    created_by_user_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: platform_experience_versions platform_experience_versions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experience_versions
    ADD CONSTRAINT platform_experience_versions_pkey PRIMARY KEY (id);


--
-- Name: platform_experience_versions uq_platform_exp_version; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experience_versions
    ADD CONSTRAINT uq_platform_exp_version UNIQUE (platform_experience_id, version_number);


--
-- Name: idx_platform_exp_ver_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_platform_exp_ver_exp ON public.platform_experience_versions USING btree (platform_experience_id);


--
-- Name: platform_experience_versions platform_experience_versions_platform_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experience_versions
    ADD CONSTRAINT platform_experience_versions_platform_experience_id_fkey FOREIGN KEY (platform_experience_id) REFERENCES public.platform_experiences(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict regifooKbzjSAr5AkCrSPyClhwe7HoHAbTlobxiHrnPLM9lMPuZ81fNe234f0M3

