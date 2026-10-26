--
-- PostgreSQL database dump
--

\restrict 3W4qawHO9zF38GWP4a00oDjFK2WclLEoNgLlfqMdTKDlakn3nSG0cltfz5cbbtP

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
-- Name: teacher_education; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_education (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    institution_name character varying(200) NOT NULL,
    degree character varying(100),
    diploma character varying(100),
    certificate character varying(100),
    field_of_study character varying(200),
    start_date date,
    end_date date,
    graduation_date date,
    country character varying(100),
    verification_status character varying(50),
    document_id uuid,
    notes text
);


--
-- Name: teacher_education teacher_education_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_education
    ADD CONSTRAINT teacher_education_pkey PRIMARY KEY (id);


--
-- Name: teacher_education fk_education_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_education
    ADD CONSTRAINT fk_education_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 3W4qawHO9zF38GWP4a00oDjFK2WclLEoNgLlfqMdTKDlakn3nSG0cltfz5cbbtP

