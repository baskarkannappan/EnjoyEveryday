--
-- PostgreSQL database dump
--

\restrict x8ww3fMaTjM5mDLWtiYdgMVrOscZRRCWvfoxP9MvVhpvCicFaQvrOkq5klGMrXo

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
-- Name: teacher_schedules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_schedules (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    day_of_week character varying(50) NOT NULL,
    start_time time without time zone NOT NULL,
    end_time time without time zone NOT NULL,
    shift_type character varying(100),
    is_active boolean DEFAULT true,
    notes text
);


--
-- Name: teacher_schedules teacher_schedules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_schedules
    ADD CONSTRAINT teacher_schedules_pkey PRIMARY KEY (id);


--
-- Name: teacher_schedules fk_schedule_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_schedules
    ADD CONSTRAINT fk_schedule_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict x8ww3fMaTjM5mDLWtiYdgMVrOscZRRCWvfoxP9MvVhpvCicFaQvrOkq5klGMrXo

