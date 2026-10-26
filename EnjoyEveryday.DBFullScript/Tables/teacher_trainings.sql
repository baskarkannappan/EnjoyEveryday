--
-- PostgreSQL database dump
--

\restrict 2lpm1aKSVtvE4fKszuoUM19u5KnLSi1JOtdcXHwkKZq5yFZhKfxxZ6cPRQx3bZS

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
-- Name: teacher_trainings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_trainings (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    training_name character varying(200) NOT NULL,
    is_mandatory boolean DEFAULT false,
    completion_date date,
    expiry_date date,
    status character varying(50)
);


--
-- Name: teacher_trainings teacher_trainings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_trainings
    ADD CONSTRAINT teacher_trainings_pkey PRIMARY KEY (id);


--
-- Name: teacher_trainings fk_training_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_trainings
    ADD CONSTRAINT fk_training_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 2lpm1aKSVtvE4fKszuoUM19u5KnLSi1JOtdcXHwkKZq5yFZhKfxxZ6cPRQx3bZS

