--
-- PostgreSQL database dump
--

\restrict eJg1mLd0v44E8cJ3mwP7nn94F7HIgrZ8NdEU01e58BsOfrk1PNVV1nhwkC6jqE1

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
-- Name: child_communication_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_communication_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    primary_language character varying(100),
    secondary_language character varying(100),
    communication_method character varying(200),
    speech_notes text,
    communication_support text,
    interpreter_required boolean DEFAULT false NOT NULL
);


--
-- Name: child_communication_profiles child_communication_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_communication_profiles
    ADD CONSTRAINT child_communication_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_communication_profiles child_communication_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_communication_profiles
    ADD CONSTRAINT child_communication_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_communication_profiles child_communication_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_communication_profiles
    ADD CONSTRAINT child_communication_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict eJg1mLd0v44E8cJ3mwP7nn94F7HIgrZ8NdEU01e58BsOfrk1PNVV1nhwkC6jqE1

