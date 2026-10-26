--
-- PostgreSQL database dump
--

\restrict DKtYvvs8JSjQTj7dFw8ohrR3OrsaMIH6zgbesZsxadabLQPceRluX9gRkY7sdnN

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
-- Name: teacher_experiences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_experiences (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    organization_name character varying(200) NOT NULL,
    "position" character varying(100) NOT NULL,
    start_date date,
    end_date date,
    age_group character varying(100),
    description text,
    reference_information text,
    verification_status character varying(50)
);


--
-- Name: teacher_experiences teacher_experiences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_experiences
    ADD CONSTRAINT teacher_experiences_pkey PRIMARY KEY (id);


--
-- Name: teacher_experiences fk_experience_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_experiences
    ADD CONSTRAINT fk_experience_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict DKtYvvs8JSjQTj7dFw8ohrR3OrsaMIH6zgbesZsxadabLQPceRluX9gRkY7sdnN

