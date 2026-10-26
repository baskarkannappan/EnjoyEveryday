--
-- PostgreSQL database dump
--

\restrict FeM1ly7G5o2hl8LaHhf4irojeRaS3d2iq9xtAzpNutsaeFN5lZSQpbZ83ffu09W

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
-- Name: children; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.children (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    date_of_birth date,
    classroom_id uuid,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    photo_url text,
    profile_completion_percentage integer DEFAULT 0,
    enrollment_status character varying(50) DEFAULT 'Draft'::character varying,
    draft_data text
);


--
-- Name: children children_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.children
    ADD CONSTRAINT children_pkey PRIMARY KEY (id);


--
-- Name: idx_children_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_children_classroom ON public.children USING btree (classroom_id);


--
-- Name: idx_children_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_children_tenant ON public.children USING btree (tenant_id);


--
-- Name: children children_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.children
    ADD CONSTRAINT children_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: children children_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.children
    ADD CONSTRAINT children_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict FeM1ly7G5o2hl8LaHhf4irojeRaS3d2iq9xtAzpNutsaeFN5lZSQpbZ83ffu09W

