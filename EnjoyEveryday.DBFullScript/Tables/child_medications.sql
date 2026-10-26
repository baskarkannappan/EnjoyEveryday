--
-- PostgreSQL database dump
--

\restrict UBqlsWqa4gmFDYTw6iW6bZX5y63kWWDkA8UX4FvNBGSgPZyGtHCvWGjoxKt3efU

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
-- Name: child_medications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_medications (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    medication_name character varying(200) NOT NULL,
    dosage character varying(100),
    route character varying(100),
    frequency character varying(100),
    start_date date,
    end_date date,
    prescribed_by character varying(200),
    instructions text,
    parent_authorization boolean DEFAULT false NOT NULL,
    medication_authorization_document_id uuid,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL
);


--
-- Name: child_medications child_medications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medications
    ADD CONSTRAINT child_medications_pkey PRIMARY KEY (id);


--
-- Name: child_medications child_medications_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medications
    ADD CONSTRAINT child_medications_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_medications child_medications_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medications
    ADD CONSTRAINT child_medications_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict UBqlsWqa4gmFDYTw6iW6bZX5y63kWWDkA8UX4FvNBGSgPZyGtHCvWGjoxKt3efU

