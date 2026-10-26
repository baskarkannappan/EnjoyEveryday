--
-- PostgreSQL database dump
--

\restrict lFHcIzTaKU1JLANMlzX31uRMM96Kpc7KtLI4lobyGVOxeITxb8VoKeeLVHvtSYF

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
-- Name: teacher_employments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_employments (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    employee_number character varying(50),
    job_title character varying(100),
    employment_type character varying(50),
    employment_status character varying(50),
    hire_date date,
    probation_end_date date,
    termination_date date,
    department character varying(100),
    branch_id uuid,
    primary_classroom_id uuid,
    reports_to_user_id uuid,
    work_location character varying(100),
    pay_type character varying(50),
    notes text
);


--
-- Name: teacher_employments teacher_employments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_employments
    ADD CONSTRAINT teacher_employments_pkey PRIMARY KEY (id);


--
-- Name: teacher_employments fk_employment_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_employments
    ADD CONSTRAINT fk_employment_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict lFHcIzTaKU1JLANMlzX31uRMM96Kpc7KtLI4lobyGVOxeITxb8VoKeeLVHvtSYF

