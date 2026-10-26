--
-- PostgreSQL database dump
--

\restrict DDLfSLe34P5qU3I3a1nKR6wNrOoow9xtyGzmcQ7RgpJHUd2aihnsEicPsUK9Veg

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
-- Name: child_toileting_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_toileting_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    toilet_training_status character varying(100),
    diaper_required boolean DEFAULT false NOT NULL,
    diaper_size character varying(50),
    changing_instructions text,
    bathroom_assistance character varying(200),
    accident_notes text
);


--
-- Name: child_toileting_profiles child_toileting_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_toileting_profiles
    ADD CONSTRAINT child_toileting_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_toileting_profiles child_toileting_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_toileting_profiles
    ADD CONSTRAINT child_toileting_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_toileting_profiles child_toileting_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_toileting_profiles
    ADD CONSTRAINT child_toileting_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict DDLfSLe34P5qU3I3a1nKR6wNrOoow9xtyGzmcQ7RgpJHUd2aihnsEicPsUK9Veg

