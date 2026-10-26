--
-- PostgreSQL database dump
--

\restrict hSC2WImgMKtuz6PAU330surEU1OnN2ZEFzfyTszqNUqRK05EwAwj56lDYQ0PBFZ

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
-- Name: classroom_statutory_ratios; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.classroom_statutory_ratios (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid,
    age_group character varying(50) NOT NULL,
    max_children_per_educator integer NOT NULL,
    description text,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: classroom_statutory_ratios classroom_statutory_ratios_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_statutory_ratios
    ADD CONSTRAINT classroom_statutory_ratios_pkey PRIMARY KEY (id);


--
-- Name: classroom_statutory_ratios classroom_statutory_ratios_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_statutory_ratios
    ADD CONSTRAINT classroom_statutory_ratios_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict hSC2WImgMKtuz6PAU330surEU1OnN2ZEFzfyTszqNUqRK05EwAwj56lDYQ0PBFZ

