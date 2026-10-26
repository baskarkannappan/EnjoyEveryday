--
-- PostgreSQL database dump
--

\restrict eRW7qkFZdJw96wZDGaDFX1PxENWNOGWcSX6exmH71CbpYuseNqqWaPI4tZrqKBc

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
-- Name: consent_choices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.consent_choices (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    consent_document_id uuid NOT NULL,
    consent_signer_id uuid,
    section_key character varying(100) NOT NULL,
    section_title character varying(255) NOT NULL,
    is_mandatory boolean DEFAULT false NOT NULL,
    choice character varying(50) NOT NULL,
    recorded_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: consent_choices consent_choices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_choices
    ADD CONSTRAINT consent_choices_pkey PRIMARY KEY (id);


--
-- Name: idx_consent_choices_doc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_choices_doc ON public.consent_choices USING btree (consent_document_id);


--
-- Name: consent_choices consent_choices_consent_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_choices
    ADD CONSTRAINT consent_choices_consent_document_id_fkey FOREIGN KEY (consent_document_id) REFERENCES public.child_consent_documents(id) ON DELETE CASCADE;


--
-- Name: consent_choices consent_choices_consent_signer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_choices
    ADD CONSTRAINT consent_choices_consent_signer_id_fkey FOREIGN KEY (consent_signer_id) REFERENCES public.consent_signers(id) ON DELETE CASCADE;


--
-- Name: consent_choices consent_choices_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_choices
    ADD CONSTRAINT consent_choices_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict eRW7qkFZdJw96wZDGaDFX1PxENWNOGWcSX6exmH71CbpYuseNqqWaPI4tZrqKBc

