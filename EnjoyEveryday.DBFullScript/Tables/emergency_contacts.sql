--
-- PostgreSQL database dump
--

\restrict 7olleP7czyxwVv1mh4Pj9QWfOEBrYAyaUWjGmaW02QG1gerORrVX9xBYD6D75cm

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
-- Name: emergency_contacts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.emergency_contacts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid,
    first_name character varying(100),
    last_name character varying(100),
    relationship character varying(100),
    phone character varying(50),
    alternate_phone character varying(50),
    email character varying(255),
    address character varying(500),
    priority integer DEFAULT 1 NOT NULL,
    is_authorized_pickup boolean DEFAULT false NOT NULL,
    notes text,
    organization_id uuid,
    branch_id uuid,
    scope character varying(50) DEFAULT 'Branch'::character varying,
    category character varying(100) DEFAULT 'EmergencyServices'::character varying,
    name character varying(255),
    phone_number character varying(100),
    alternate_phone_number character varying(100),
    country_region character varying(100),
    instructions text,
    verification_status character varying(50) DEFAULT 'Unverified'::character varying,
    verified_by character varying(255),
    verified_at timestamp with time zone,
    review_due_at timestamp with time zone,
    is_active boolean DEFAULT true,
    display_order integer DEFAULT 0,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: emergency_contacts emergency_contacts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.emergency_contacts
    ADD CONSTRAINT emergency_contacts_pkey PRIMARY KEY (id);


--
-- Name: idx_emergency_contacts_lookup; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_emergency_contacts_lookup ON public.emergency_contacts USING btree (tenant_id, organization_id, branch_id, is_active);


--
-- Name: emergency_contacts emergency_contacts_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.emergency_contacts
    ADD CONSTRAINT emergency_contacts_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: emergency_contacts emergency_contacts_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.emergency_contacts
    ADD CONSTRAINT emergency_contacts_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 7olleP7czyxwVv1mh4Pj9QWfOEBrYAyaUWjGmaW02QG1gerORrVX9xBYD6D75cm

