--
-- PostgreSQL database dump
--

\restrict bVfMAtUBCO1dc4Aig5eafzy8raDlTwcTqfjaz37LsvK477X27Gk2SvFMQVHznGG

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
-- Name: consent_signers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.consent_signers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    consent_document_id uuid NOT NULL,
    family_relationship_id uuid,
    person_id uuid,
    intended_signer_user_id uuid,
    signer_name_snapshot character varying(255) NOT NULL,
    signer_email_snapshot character varying(255) NOT NULL,
    signer_relationship character varying(100) NOT NULL,
    is_required boolean DEFAULT true NOT NULL,
    signing_status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    token_hash character varying(128),
    token_expires_at timestamp with time zone,
    invitation_sent_count integer DEFAULT 0 NOT NULL,
    last_invitation_sent_at timestamp with time zone,
    last_opened_at timestamp with time zone,
    signed_at timestamp with time zone,
    signature_legal_name character varying(255),
    signature_intent_text text,
    signature_ip_address character varying(100),
    signature_user_agent text
);


--
-- Name: consent_signers consent_signers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_pkey PRIMARY KEY (id);


--
-- Name: idx_consent_signers_doc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_signers_doc ON public.consent_signers USING btree (consent_document_id);


--
-- Name: idx_consent_signers_token; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_signers_token ON public.consent_signers USING btree (token_hash);


--
-- Name: consent_signers consent_signers_consent_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_consent_document_id_fkey FOREIGN KEY (consent_document_id) REFERENCES public.child_consent_documents(id) ON DELETE CASCADE;


--
-- Name: consent_signers consent_signers_family_relationship_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_family_relationship_id_fkey FOREIGN KEY (family_relationship_id) REFERENCES public.family_relationships(id);


--
-- Name: consent_signers consent_signers_intended_signer_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_intended_signer_user_id_fkey FOREIGN KEY (intended_signer_user_id) REFERENCES public.users(id);


--
-- Name: consent_signers consent_signers_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.persons(id);


--
-- Name: consent_signers consent_signers_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict bVfMAtUBCO1dc4Aig5eafzy8raDlTwcTqfjaz37LsvK477X27Gk2SvFMQVHznGG

