--
-- PostgreSQL database dump
--

\restrict Yn2rG89LOF3WrDQmYbradWYhvHILoROBUerdOUBCNaiWFFXd5zmzRl7HzIMKWte

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
-- Name: teacher_attendance; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_attendance (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    attendance_date date NOT NULL,
    status character varying(50) NOT NULL,
    clock_in_time time without time zone,
    clock_out_time time without time zone,
    notes text,
    recorded_by_user_id uuid,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: teacher_attendance teacher_attendance_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_attendance
    ADD CONSTRAINT teacher_attendance_pkey PRIMARY KEY (id);


--
-- Name: teacher_attendance fk_ta_branch; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_attendance
    ADD CONSTRAINT fk_ta_branch FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: teacher_attendance fk_ta_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_attendance
    ADD CONSTRAINT fk_ta_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id);


--
-- Name: teacher_attendance fk_ta_tenant; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_attendance
    ADD CONSTRAINT fk_ta_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict Yn2rG89LOF3WrDQmYbradWYhvHILoROBUerdOUBCNaiWFFXd5zmzRl7HzIMKWte

