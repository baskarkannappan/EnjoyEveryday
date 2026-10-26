--
-- PostgreSQL database dump
--

\restrict VABeFHdtHdGlTeBuKFqazCweaHP92NNPtJUM7y4bPLXUsakNh9qubfLRVgch4ha

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
-- Name: experience_session_teachers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_session_teachers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    experience_session_id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    role character varying(50) DEFAULT 'LeadTeacher'::character varying NOT NULL
);


--
-- Name: experience_session_teachers experience_session_teachers_experience_session_id_teacher_i_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_session_teachers
    ADD CONSTRAINT experience_session_teachers_experience_session_id_teacher_i_key UNIQUE (experience_session_id, teacher_id);


--
-- Name: experience_session_teachers experience_session_teachers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_session_teachers
    ADD CONSTRAINT experience_session_teachers_pkey PRIMARY KEY (id);


--
-- Name: idx_exp_sess_teachers_session; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_sess_teachers_session ON public.experience_session_teachers USING btree (experience_session_id);


--
-- Name: experience_session_teachers experience_session_teachers_experience_session_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_session_teachers
    ADD CONSTRAINT experience_session_teachers_experience_session_id_fkey FOREIGN KEY (experience_session_id) REFERENCES public.experience_sessions(id) ON DELETE CASCADE;


--
-- Name: experience_session_teachers experience_session_teachers_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_session_teachers
    ADD CONSTRAINT experience_session_teachers_teacher_id_fkey FOREIGN KEY (teacher_id) REFERENCES public.users(id);


--
-- PostgreSQL database dump complete
--

\unrestrict VABeFHdtHdGlTeBuKFqazCweaHP92NNPtJUM7y4bPLXUsakNh9qubfLRVgch4ha

