--
-- PostgreSQL database dump
--

\restrict Ceqa8Sxf1IA1wzHzcMragRArIaIdVpZD158yyOcm8aqQTI98ItbPvXegGli8Lcs

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
-- Name: teacher_emergency_contacts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_emergency_contacts (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    relationship character varying(100),
    primary_phone character varying(50),
    alternate_phone character varying(50),
    email character varying(200),
    address text,
    priority integer DEFAULT 1,
    notes text
);


--
-- Name: teacher_emergency_contacts teacher_emergency_contacts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_emergency_contacts
    ADD CONSTRAINT teacher_emergency_contacts_pkey PRIMARY KEY (id);


--
-- Name: teacher_emergency_contacts fk_emergency_contact_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_emergency_contacts
    ADD CONSTRAINT fk_emergency_contact_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict Ceqa8Sxf1IA1wzHzcMragRArIaIdVpZD158yyOcm8aqQTI98ItbPvXegGli8Lcs

