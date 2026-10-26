--
-- PostgreSQL database dump
--

\restrict IjMcOtCy8csfNxX9DJ30188TCArMNHNR5JIKLfcZBzo9MAK5XxyGzcSB4Xvwt1c

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
-- Name: teacher_specialties; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_specialties (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    specialty_id uuid,
    level character varying(100),
    years_experience integer,
    notes text
);


--
-- Name: teacher_specialties teacher_specialties_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_specialties
    ADD CONSTRAINT teacher_specialties_pkey PRIMARY KEY (id);


--
-- Name: teacher_specialties fk_specialty_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_specialties
    ADD CONSTRAINT fk_specialty_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict IjMcOtCy8csfNxX9DJ30188TCArMNHNR5JIKLfcZBzo9MAK5XxyGzcSB4Xvwt1c

