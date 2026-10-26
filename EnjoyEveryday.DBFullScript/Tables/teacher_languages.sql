--
-- PostgreSQL database dump
--

\restrict PfhtZct7LKISn7Dp8aOek9zteg2RobhWpB1oKQrqcM6YLnej6iNncSkRaDiyueE

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
-- Name: teacher_languages; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_languages (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    language character varying(100) NOT NULL,
    proficiency character varying(50),
    is_primary_language boolean DEFAULT false,
    can_communicate_with_parents boolean DEFAULT false,
    can_teach_in_language boolean DEFAULT false
);


--
-- Name: teacher_languages teacher_languages_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_languages
    ADD CONSTRAINT teacher_languages_pkey PRIMARY KEY (id);


--
-- Name: teacher_languages fk_language_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_languages
    ADD CONSTRAINT fk_language_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict PfhtZct7LKISn7Dp8aOek9zteg2RobhWpB1oKQrqcM6YLnej6iNncSkRaDiyueE

