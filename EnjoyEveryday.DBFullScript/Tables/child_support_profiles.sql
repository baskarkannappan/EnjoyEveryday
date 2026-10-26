--
-- PostgreSQL database dump
--

\restrict n8DzVPGtnqlTMOyCda8iE1h2nfNxmC5u8fPxTZMCWPpO7iTtKPW4AjCGTVyJUjM

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
-- Name: child_support_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_support_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    support_required boolean DEFAULT false NOT NULL,
    support_description text,
    learning_preferences text,
    accessibility_needs text,
    communication_support text,
    sensory_considerations text,
    behavior_support_plan text,
    external_support_provider character varying(255),
    notes text
);


--
-- Name: child_support_profiles child_support_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_support_profiles
    ADD CONSTRAINT child_support_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_support_profiles child_support_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_support_profiles
    ADD CONSTRAINT child_support_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_support_profiles child_support_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_support_profiles
    ADD CONSTRAINT child_support_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict n8DzVPGtnqlTMOyCda8iE1h2nfNxmC5u8fPxTZMCWPpO7iTtKPW4AjCGTVyJUjM

