--
-- PostgreSQL database dump
--

\restrict ypfvS0s8UI2EQS9hx9lAZVOvZtK4Uv7LOqNwSNyGEWSTTgfgxamDoAdca6aojP4

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
-- Name: event_experiences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.event_experiences (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    event_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    experience_schedule_id uuid,
    experience_session_id uuid
);


--
-- Name: event_experiences event_experiences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_pkey PRIMARY KEY (id);


--
-- Name: idx_event_experiences_event; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_event_experiences_event ON public.event_experiences USING btree (event_id);


--
-- Name: event_experiences event_experiences_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE CASCADE;


--
-- Name: event_experiences event_experiences_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id);


--
-- Name: event_experiences event_experiences_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id);


--
-- Name: event_experiences event_experiences_experience_session_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_experience_session_id_fkey FOREIGN KEY (experience_session_id) REFERENCES public.experience_sessions(id);


--
-- PostgreSQL database dump complete
--

\unrestrict ypfvS0s8UI2EQS9hx9lAZVOvZtK4Uv7LOqNwSNyGEWSTTgfgxamDoAdca6aojP4

