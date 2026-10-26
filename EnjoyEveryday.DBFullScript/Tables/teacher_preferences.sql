--
-- PostgreSQL database dump
--

\restrict 6wMCXqgCKHWGDcJ5vSVmjZ8hWvR36fuwE10GClgDDDg6YGeSvv6iyTuPWuxsIOl

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
-- Name: teacher_preferences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_preferences (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    preference_type character varying(100) NOT NULL,
    preference_value character varying(200) NOT NULL,
    notes text
);


--
-- Name: teacher_preferences teacher_preferences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_preferences
    ADD CONSTRAINT teacher_preferences_pkey PRIMARY KEY (id);


--
-- Name: teacher_preferences fk_preference_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_preferences
    ADD CONSTRAINT fk_preference_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 6wMCXqgCKHWGDcJ5vSVmjZ8hWvR36fuwE10GClgDDDg6YGeSvv6iyTuPWuxsIOl

