--
-- PostgreSQL database dump
--

\restrict 1cTtisw0Osn0XEfZSorshwVSpFf4tYdBE5uneQigcFr0ox9Y5Hie8Y9azjcsKjc

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
-- Name: teacher_availabilities; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_availabilities (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    can_work_mornings boolean DEFAULT false,
    can_work_afternoons boolean DEFAULT false,
    can_work_evenings boolean DEFAULT false,
    can_work_weekends boolean DEFAULT false,
    max_hours_per_week numeric(5,2),
    notes text
);


--
-- Name: teacher_availabilities teacher_availabilities_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_availabilities
    ADD CONSTRAINT teacher_availabilities_pkey PRIMARY KEY (id);


--
-- Name: teacher_availabilities fk_availability_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_availabilities
    ADD CONSTRAINT fk_availability_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 1cTtisw0Osn0XEfZSorshwVSpFf4tYdBE5uneQigcFr0ox9Y5Hie8Y9azjcsKjc

