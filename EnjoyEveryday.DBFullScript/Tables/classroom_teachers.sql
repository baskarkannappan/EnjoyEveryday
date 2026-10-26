--
-- PostgreSQL database dump
--

\restrict UmuV8o0v3c8nYKqBk8UvDrl6K9MuIUBVHCzt6XBJAzg5kgPNuJ8oHPZwrIATNto

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
-- Name: classroom_teachers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.classroom_teachers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    user_id uuid NOT NULL,
    is_primary boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: classroom_teachers classroom_teachers_classroom_id_user_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_classroom_id_user_id_key UNIQUE (classroom_id, user_id);


--
-- Name: classroom_teachers classroom_teachers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_pkey PRIMARY KEY (id);


--
-- Name: idx_classroom_teachers_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_classroom_teachers_tenant ON public.classroom_teachers USING btree (tenant_id);


--
-- Name: classroom_teachers classroom_teachers_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: classroom_teachers classroom_teachers_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: classroom_teachers classroom_teachers_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict UmuV8o0v3c8nYKqBk8UvDrl6K9MuIUBVHCzt6XBJAzg5kgPNuJ8oHPZwrIATNto

