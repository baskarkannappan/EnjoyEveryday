--
-- PostgreSQL database dump
--

\restrict aNafhbrQvQvPfa3QGS2nLNuBYvk0n9SdkDjizafXWV0GHqbojcPRIchhOdlYiGM

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
-- Name: teacher_skills; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_skills (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    skill_id uuid NOT NULL,
    skill_level character varying(50),
    years_experience integer,
    is_primary_skill boolean DEFAULT false,
    verified boolean DEFAULT false,
    verified_by uuid,
    verified_at timestamp with time zone,
    notes text
);


--
-- Name: teacher_skills teacher_skills_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_skills
    ADD CONSTRAINT teacher_skills_pkey PRIMARY KEY (id);


--
-- Name: teacher_skills fk_teacher_skills_skill; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_skills
    ADD CONSTRAINT fk_teacher_skills_skill FOREIGN KEY (skill_id) REFERENCES public.skills(id) ON DELETE CASCADE;


--
-- Name: teacher_skills fk_teacher_skills_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_skills
    ADD CONSTRAINT fk_teacher_skills_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict aNafhbrQvQvPfa3QGS2nLNuBYvk0n9SdkDjizafXWV0GHqbojcPRIchhOdlYiGM

