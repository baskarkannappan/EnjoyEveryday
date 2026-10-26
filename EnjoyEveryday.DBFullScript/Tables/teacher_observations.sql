--
-- PostgreSQL database dump
--

\restrict oAyaMgd3aNXhf0dolooii7BfcerwOCp0fKsxAyQYMOpop80Er8Qp6ebg8YCKnLC

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
-- Name: teacher_observations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_observations (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    observer_user_id uuid NOT NULL,
    observation_date date NOT NULL,
    rating character varying(50),
    feedback text,
    action_items text
);


--
-- Name: teacher_observations teacher_observations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_observations
    ADD CONSTRAINT teacher_observations_pkey PRIMARY KEY (id);


--
-- Name: teacher_observations fk_observation_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_observations
    ADD CONSTRAINT fk_observation_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict oAyaMgd3aNXhf0dolooii7BfcerwOCp0fKsxAyQYMOpop80Er8Qp6ebg8YCKnLC

