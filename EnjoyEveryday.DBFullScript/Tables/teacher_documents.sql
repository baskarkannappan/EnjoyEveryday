--
-- PostgreSQL database dump
--

\restrict xsP1rlny2gthXutLAnnuTAWGfZCq47fFYgXfLo8tNmUOXBY9wzH9wZbWrR1PLCy

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
-- Name: teacher_documents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_documents (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    document_type character varying(100) NOT NULL,
    file_name character varying(200) NOT NULL,
    file_path text NOT NULL,
    uploaded_at timestamp with time zone DEFAULT now() NOT NULL,
    notes text
);


--
-- Name: teacher_documents teacher_documents_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_documents
    ADD CONSTRAINT teacher_documents_pkey PRIMARY KEY (id);


--
-- Name: teacher_documents fk_doc_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_documents
    ADD CONSTRAINT fk_doc_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict xsP1rlny2gthXutLAnnuTAWGfZCq47fFYgXfLo8tNmUOXBY9wzH9wZbWrR1PLCy

