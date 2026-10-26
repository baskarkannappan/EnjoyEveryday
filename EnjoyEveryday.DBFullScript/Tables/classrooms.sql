--
-- PostgreSQL database dump
--

\restrict R15TA998fMK3QFmuqd12NvF23xUwrpjceeRyDxQCaXZSBSqywlMx335Y4uiIyLa

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
-- Name: classrooms; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.classrooms (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    name character varying(200) NOT NULL,
    age_group character varying(100),
    capacity integer,
    environment character varying(50) DEFAULT 'Indoor'::character varying,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    display_name character varying(255),
    short_name character varying(50),
    code character varying(50),
    description text,
    classroom_type character varying(100),
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    min_age_months integer,
    max_age_months integer,
    current_enrollment integer DEFAULT 0,
    photo_url text,
    draft_data jsonb,
    profile_completion_percentage integer DEFAULT 0,
    created_by uuid,
    updated_by uuid
);


--
-- Name: classrooms classrooms_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classrooms
    ADD CONSTRAINT classrooms_pkey PRIMARY KEY (id);


--
-- Name: idx_classrooms_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_classrooms_branch ON public.classrooms USING btree (branch_id);


--
-- Name: idx_classrooms_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_classrooms_status ON public.classrooms USING btree (status);


--
-- Name: idx_classrooms_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_classrooms_tenant ON public.classrooms USING btree (tenant_id);


--
-- Name: classrooms classrooms_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classrooms
    ADD CONSTRAINT classrooms_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: classrooms classrooms_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classrooms
    ADD CONSTRAINT classrooms_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict R15TA998fMK3QFmuqd12NvF23xUwrpjceeRyDxQCaXZSBSqywlMx335Y4uiIyLa

