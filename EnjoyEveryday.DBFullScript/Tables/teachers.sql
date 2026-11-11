--
-- PostgreSQL database dump
--

\restrict vW0ue12T6ZMy1aNEDFE7Zvcks5CewYdBfxqElv5uExr0QK3XZ4FgQEDNDpdoB0V

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
-- Name: teachers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teachers (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    person_id uuid,
    user_id uuid,
    employee_number character varying(50),
    first_name character varying(100) NOT NULL,
    middle_name character varying(100),
    last_name character varying(100) NOT NULL,
    preferred_name character varying(100),
    display_name character varying(200),
    profile_photo_url text,
    date_of_birth date,
    gender character varying(50),
    preferred_language character varying(50),
    other_languages text,
    pronouns character varying(50),
    bio text,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    draft_data jsonb,
    profile_completion_percentage integer DEFAULT 0,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: teachers teachers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teachers
    ADD CONSTRAINT teachers_pkey PRIMARY KEY (id);


--
-- Name: teachers fk_teacher_tenant; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teachers
    ADD CONSTRAINT fk_teacher_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict vW0ue12T6ZMy1aNEDFE7Zvcks5CewYdBfxqElv5uExr0QK3XZ4FgQEDNDpdoB0V

