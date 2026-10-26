--
-- PostgreSQL database dump
--

\restrict kIeBDkGFVZjhaEGvwwwNlkzPCZ6CbWjYAbDqTpd34OcRENu0ARnvLGFqdbvtWZg

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
-- Name: child_consent_documents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_consent_documents (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid,
    branch_id uuid,
    classroom_id uuid,
    child_id uuid NOT NULL,
    consent_template_id uuid NOT NULL,
    template_version character varying(50) NOT NULL,
    document_version integer DEFAULT 1 NOT NULL,
    document_title character varying(255) NOT NULL,
    document_snapshot text NOT NULL,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    signing_deadline timestamp with time zone,
    created_by uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    admin_reviewed_by uuid,
    admin_reviewed_at timestamp with time zone,
    admin_review_notes text,
    approved_at timestamp with time zone,
    sent_at timestamp with time zone,
    signed_at timestamp with time zone,
    declined_at timestamp with time zone,
    withdrawn_at timestamp with time zone,
    signed_document_storage_key character varying(500),
    signed_document_checksum character varying(128),
    supersedes_document_id uuid,
    row_version integer DEFAULT 1 NOT NULL
);


--
-- Name: child_consent_documents child_consent_documents_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_pkey PRIMARY KEY (id);


--
-- Name: idx_child_consent_docs_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_consent_docs_child ON public.child_consent_documents USING btree (tenant_id, child_id);


--
-- Name: idx_child_consent_docs_org_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_consent_docs_org_branch ON public.child_consent_documents USING btree (organization_id, branch_id);


--
-- Name: idx_child_consent_docs_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_consent_docs_status ON public.child_consent_documents USING btree (tenant_id, status);


--
-- Name: child_consent_documents child_consent_documents_admin_reviewed_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_admin_reviewed_by_fkey FOREIGN KEY (admin_reviewed_by) REFERENCES public.users(id);


--
-- Name: child_consent_documents child_consent_documents_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_consent_documents child_consent_documents_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_consent_documents child_consent_documents_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_consent_documents child_consent_documents_consent_template_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_consent_template_id_fkey FOREIGN KEY (consent_template_id) REFERENCES public.consent_templates(id);


--
-- Name: child_consent_documents child_consent_documents_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(id);


--
-- Name: child_consent_documents child_consent_documents_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_consent_documents child_consent_documents_supersedes_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_supersedes_document_id_fkey FOREIGN KEY (supersedes_document_id) REFERENCES public.child_consent_documents(id);


--
-- Name: child_consent_documents child_consent_documents_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict kIeBDkGFVZjhaEGvwwwNlkzPCZ6CbWjYAbDqTpd34OcRENu0ARnvLGFqdbvtWZg

