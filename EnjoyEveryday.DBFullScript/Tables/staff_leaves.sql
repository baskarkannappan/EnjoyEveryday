--
-- PostgreSQL database dump
--

\restrict r6qYD4DhQSYqbjb0Arbzo5ffnjXF6RYAp5yUzUekSnmdQwYzanOpMwtzbtupCcC

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
-- Name: staff_leaves; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.staff_leaves (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    leave_type character varying(50) NOT NULL,
    start_date date NOT NULL,
    end_date date NOT NULL,
    reason text,
    status character varying(50) DEFAULT 'Approved'::character varying NOT NULL,
    coverage_status character varying(50) DEFAULT 'Uncovered'::character varying NOT NULL,
    substitute_teacher_id uuid,
    reviewed_by_user_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: staff_leaves staff_leaves_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_pkey PRIMARY KEY (id);


--
-- Name: idx_staff_leaves_teacher; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_staff_leaves_teacher ON public.staff_leaves USING btree (teacher_id);


--
-- Name: idx_staff_leaves_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_staff_leaves_tenant ON public.staff_leaves USING btree (tenant_id, start_date, end_date);


--
-- Name: staff_leaves staff_leaves_reviewed_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_reviewed_by_user_id_fkey FOREIGN KEY (reviewed_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: staff_leaves staff_leaves_substitute_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_substitute_teacher_id_fkey FOREIGN KEY (substitute_teacher_id) REFERENCES public.teachers(id) ON DELETE SET NULL;


--
-- Name: staff_leaves staff_leaves_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_teacher_id_fkey FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: staff_leaves staff_leaves_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict r6qYD4DhQSYqbjb0Arbzo5ffnjXF6RYAp5yUzUekSnmdQwYzanOpMwtzbtupCcC

