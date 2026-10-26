--
-- PostgreSQL database dump
--

\restrict 8RhQWXOPrI6Ir7GZpf17r2KRfKMTaf1wGhVNkuBMffDirHFld06jLyyhANtShpg

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
-- Name: child_feeding_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_feeding_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    feeding_method character varying(100),
    bottle_type character varying(100),
    bottle_amount character varying(100),
    bottle_schedule character varying(200),
    solid_food_stage character varying(100),
    feeding_assistance character varying(100),
    choking_risk text,
    feeding_instructions text
);


--
-- Name: child_feeding_profiles child_feeding_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_feeding_profiles
    ADD CONSTRAINT child_feeding_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_feeding_profiles child_feeding_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_feeding_profiles
    ADD CONSTRAINT child_feeding_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_feeding_profiles child_feeding_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_feeding_profiles
    ADD CONSTRAINT child_feeding_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 8RhQWXOPrI6Ir7GZpf17r2KRfKMTaf1wGhVNkuBMffDirHFld06jLyyhANtShpg

