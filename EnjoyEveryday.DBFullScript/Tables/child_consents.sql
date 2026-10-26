--
-- PostgreSQL database dump
--

\restrict qHqViYtHdjlOYHNuqDgbMjRBKUxbEXGq6066DG9uJNHlinmZVKldw7Ls9QOu6XQ

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
-- Name: child_consents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_consents (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    consent_type character varying(100) NOT NULL,
    granted boolean DEFAULT false NOT NULL,
    granted_by character varying(255),
    granted_at timestamp with time zone,
    expires_at timestamp with time zone,
    document_id uuid,
    version character varying(50)
);


--
-- Name: child_consents child_consents_child_id_consent_type_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_child_id_consent_type_key UNIQUE (child_id, consent_type);


--
-- Name: child_consents child_consents_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_pkey PRIMARY KEY (id);


--
-- Name: child_consents child_consents_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_consents child_consents_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.child_documents(id);


--
-- Name: child_consents child_consents_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict qHqViYtHdjlOYHNuqDgbMjRBKUxbEXGq6066DG9uJNHlinmZVKldw7Ls9QOu6XQ

