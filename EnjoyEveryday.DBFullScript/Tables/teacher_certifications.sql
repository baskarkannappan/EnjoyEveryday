--
-- PostgreSQL database dump
--

\restrict CXvPC9e3g9UcOOzZbRCkETmjHOsTiVaC4aK8UNmfht6HJ46d9bkQMqByvhoVtMI

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
-- Name: teacher_certifications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_certifications (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    certification_type character varying(100),
    certification_name character varying(200) NOT NULL,
    issuing_authority character varying(200),
    certificate_number character varying(100),
    issue_date date,
    expiration_date date,
    status character varying(50),
    verification_status character varying(50),
    document_id uuid,
    notes text
);


--
-- Name: teacher_certifications teacher_certifications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_certifications
    ADD CONSTRAINT teacher_certifications_pkey PRIMARY KEY (id);


--
-- Name: teacher_certifications fk_certification_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_certifications
    ADD CONSTRAINT fk_certification_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict CXvPC9e3g9UcOOzZbRCkETmjHOsTiVaC4aK8UNmfht6HJ46d9bkQMqByvhoVtMI

