--
-- PostgreSQL database dump
--

\restrict nUNzLDthQzQaOB511B5RVsf44UoAYcLVHGg8dKhcOXLofaPpo3ETkAxCvLEPkMK

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
-- Name: child_legal_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_legal_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    has_custody_restrictions boolean DEFAULT false NOT NULL,
    legal_notes text,
    court_order_document_id uuid
);


--
-- Name: child_legal_profiles child_legal_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_legal_profiles
    ADD CONSTRAINT child_legal_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_legal_profiles child_legal_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_legal_profiles
    ADD CONSTRAINT child_legal_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_legal_profiles child_legal_profiles_court_order_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_legal_profiles
    ADD CONSTRAINT child_legal_profiles_court_order_document_id_fkey FOREIGN KEY (court_order_document_id) REFERENCES public.child_documents(id);


--
-- Name: child_legal_profiles child_legal_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_legal_profiles
    ADD CONSTRAINT child_legal_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict nUNzLDthQzQaOB511B5RVsf44UoAYcLVHGg8dKhcOXLofaPpo3ETkAxCvLEPkMK

