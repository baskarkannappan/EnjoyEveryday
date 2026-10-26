--
-- PostgreSQL database dump
--

\restrict hhTSbRchxd5cXAXefeiAdqYIgyadyMG0eeb1ygLx6oagcr31eYdG9fc1sJNyNQP

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
-- Name: child_dietary_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_dietary_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    diet_type character varying(100),
    food_restrictions text,
    religious_restrictions text,
    texture_restrictions text,
    feeding_notes text,
    self_feeds boolean DEFAULT true NOT NULL,
    special_instructions text
);


--
-- Name: child_dietary_profiles child_dietary_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_dietary_profiles
    ADD CONSTRAINT child_dietary_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_dietary_profiles child_dietary_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_dietary_profiles
    ADD CONSTRAINT child_dietary_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_dietary_profiles child_dietary_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_dietary_profiles
    ADD CONSTRAINT child_dietary_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict hhTSbRchxd5cXAXefeiAdqYIgyadyMG0eeb1ygLx6oagcr31eYdG9fc1sJNyNQP

