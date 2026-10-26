--
-- PostgreSQL database dump
--

\restrict eAPVh3xiF7K3jyv89WA3axOnRMsEDxYmXUzfb5FQBWRFuskafSOPaMs9QuOpQCu

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
-- Name: event_classrooms; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.event_classrooms (
    event_id uuid NOT NULL,
    classroom_id uuid NOT NULL
);


--
-- Name: event_classrooms event_classrooms_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_classrooms
    ADD CONSTRAINT event_classrooms_pkey PRIMARY KEY (event_id, classroom_id);


--
-- Name: event_classrooms event_classrooms_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_classrooms
    ADD CONSTRAINT event_classrooms_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: event_classrooms event_classrooms_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_classrooms
    ADD CONSTRAINT event_classrooms_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict eAPVh3xiF7K3jyv89WA3axOnRMsEDxYmXUzfb5FQBWRFuskafSOPaMs9QuOpQCu

