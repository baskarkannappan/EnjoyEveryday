--
-- PostgreSQL database dump
--

\restrict 7aMvVP1wb87rATN4xtzo2YppJhrImRalDOG8WamklQUvssLYTZnWdiuaqTJ4C7J

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
-- Name: child_medical_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_medical_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    primary_physician character varying(200),
    physician_phone character varying(50),
    clinic_name character varying(200),
    clinic_phone character varying(50),
    health_card_number character varying(100),
    blood_type character varying(20),
    medical_notes text,
    special_medical_needs text,
    medical_consent boolean DEFAULT false NOT NULL
);


--
-- Name: child_medical_profiles child_medical_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medical_profiles
    ADD CONSTRAINT child_medical_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_medical_profiles child_medical_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medical_profiles
    ADD CONSTRAINT child_medical_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_medical_profiles child_medical_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medical_profiles
    ADD CONSTRAINT child_medical_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 7aMvVP1wb87rATN4xtzo2YppJhrImRalDOG8WamklQUvssLYTZnWdiuaqTJ4C7J

