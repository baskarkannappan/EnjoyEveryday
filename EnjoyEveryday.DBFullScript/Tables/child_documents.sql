--
-- PostgreSQL database dump
--

\restrict fSzV3Vw2bUTQAHqTV1KRi9th0XA3MgCtAXqKfBqVuIaahAbxy309vsuKm03VYNf

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
-- Name: child_documents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_documents (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    document_type character varying(100) NOT NULL,
    file_name character varying(255) NOT NULL,
    storage_url character varying(1024) NOT NULL,
    uploaded_by uuid,
    uploaded_at timestamp with time zone DEFAULT now() NOT NULL,
    expiration_date date,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL
);


--
-- Name: child_documents child_documents_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_documents
    ADD CONSTRAINT child_documents_pkey PRIMARY KEY (id);


--
-- Name: child_documents child_documents_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_documents
    ADD CONSTRAINT child_documents_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_documents child_documents_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_documents
    ADD CONSTRAINT child_documents_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_documents child_documents_uploaded_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_documents
    ADD CONSTRAINT child_documents_uploaded_by_fkey FOREIGN KEY (uploaded_by) REFERENCES public.users(id);


--
-- PostgreSQL database dump complete
--

\unrestrict fSzV3Vw2bUTQAHqTV1KRi9th0XA3MgCtAXqKfBqVuIaahAbxy309vsuKm03VYNf

