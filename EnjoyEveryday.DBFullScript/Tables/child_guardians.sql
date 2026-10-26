--
-- PostgreSQL database dump
--

\restrict lbXwfLImPqANz7k5vAOHkxY4ie4lzHkoXndFjVbzAmVrSVP57y4u4XbB9v4bGdg

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
-- Name: child_guardians; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_guardians (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    person_id uuid NOT NULL,
    relationship character varying(100) NOT NULL,
    is_primary boolean DEFAULT false NOT NULL,
    has_legal_custody boolean DEFAULT false NOT NULL,
    can_pickup boolean DEFAULT true NOT NULL,
    can_receive_information boolean DEFAULT true NOT NULL,
    can_authorize_medical_care boolean DEFAULT false NOT NULL
);


--
-- Name: child_guardians child_guardians_child_id_person_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_child_id_person_id_key UNIQUE (child_id, person_id);


--
-- Name: child_guardians child_guardians_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_pkey PRIMARY KEY (id);


--
-- Name: child_guardians child_guardians_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_guardians child_guardians_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.persons(id) ON DELETE CASCADE;


--
-- Name: child_guardians child_guardians_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict lbXwfLImPqANz7k5vAOHkxY4ie4lzHkoXndFjVbzAmVrSVP57y4u4XbB9v4bGdg

