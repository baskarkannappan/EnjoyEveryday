--
-- PostgreSQL database dump
--

\restrict WMD7gBeTqPA4odQ3FaU60c97ge2giL0htZVhTPRD6KKxfDCkYANDUa856iOCJVt

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
-- Name: experience_versions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    experience_id uuid NOT NULL,
    version_number integer NOT NULL,
    title character varying(200) NOT NULL,
    description text,
    dna_payload jsonb DEFAULT '{}'::jsonb NOT NULL,
    change_reason character varying(500),
    modified_by_user_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: experience_versions experience_versions_experience_id_version_number_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_versions
    ADD CONSTRAINT experience_versions_experience_id_version_number_key UNIQUE (experience_id, version_number);


--
-- Name: experience_versions experience_versions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_versions
    ADD CONSTRAINT experience_versions_pkey PRIMARY KEY (id);


--
-- Name: idx_exp_versions_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_versions_exp ON public.experience_versions USING btree (experience_id);


--
-- Name: experience_versions experience_versions_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_versions
    ADD CONSTRAINT experience_versions_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: experience_versions experience_versions_modified_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_versions
    ADD CONSTRAINT experience_versions_modified_by_user_id_fkey FOREIGN KEY (modified_by_user_id) REFERENCES public.users(id);


--
-- PostgreSQL database dump complete
--

\unrestrict WMD7gBeTqPA4odQ3FaU60c97ge2giL0htZVhTPRD6KKxfDCkYANDUa856iOCJVt

