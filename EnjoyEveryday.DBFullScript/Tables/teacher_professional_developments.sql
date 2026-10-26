--
-- PostgreSQL database dump
--

\restrict 6BxZyj7P5syyViLAnjEISOgQg7Gi9Rs8SBQApZ8HDL2ximaHSlhzd0ALDghcoQ9

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
-- Name: teacher_professional_developments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_professional_developments (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    course_name character varying(200) NOT NULL,
    provider character varying(200),
    hours_completed numeric(5,2),
    completion_date date,
    notes text
);


--
-- Name: teacher_professional_developments teacher_professional_developments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_professional_developments
    ADD CONSTRAINT teacher_professional_developments_pkey PRIMARY KEY (id);


--
-- Name: teacher_professional_developments fk_pd_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_professional_developments
    ADD CONSTRAINT fk_pd_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 6BxZyj7P5syyViLAnjEISOgQg7Gi9Rs8SBQApZ8HDL2ximaHSlhzd0ALDghcoQ9

