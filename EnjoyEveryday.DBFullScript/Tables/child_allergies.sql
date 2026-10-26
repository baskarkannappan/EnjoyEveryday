--
-- PostgreSQL database dump
--

\restrict aDas6o5Y6yLLxFXUGeA72TzbIL9GL1ciEWAojD811mbDhfsOUAdJT1069glVQFQ

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
-- Name: child_allergies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_allergies (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    allergen character varying(200) NOT NULL,
    reaction character varying(200),
    severity character varying(50) NOT NULL,
    symptoms text,
    treatment text,
    emergency_action text,
    has_epi_pen boolean DEFAULT false NOT NULL,
    notes text
);


--
-- Name: child_allergies child_allergies_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_allergies
    ADD CONSTRAINT child_allergies_pkey PRIMARY KEY (id);


--
-- Name: child_allergies child_allergies_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_allergies
    ADD CONSTRAINT child_allergies_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_allergies child_allergies_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_allergies
    ADD CONSTRAINT child_allergies_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict aDas6o5Y6yLLxFXUGeA72TzbIL9GL1ciEWAojD811mbDhfsOUAdJT1069glVQFQ

