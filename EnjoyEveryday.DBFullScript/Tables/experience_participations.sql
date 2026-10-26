--
-- PostgreSQL database dump
--

\restrict Sl8yullChZBXLbOE4IjuRSthRnH7daTVJWIGhoxLCts8YylufElZ7TDRb2yk37g

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
-- Name: experience_participations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_participations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    experience_session_id uuid NOT NULL,
    child_id uuid NOT NULL,
    participation_status character varying(50) DEFAULT 'Participated'::character varying NOT NULL,
    participation_notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: experience_participations experience_participations_experience_session_id_child_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_participations
    ADD CONSTRAINT experience_participations_experience_session_id_child_id_key UNIQUE (experience_session_id, child_id);


--
-- Name: experience_participations experience_participations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_participations
    ADD CONSTRAINT experience_participations_pkey PRIMARY KEY (id);


--
-- Name: idx_exp_participations_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_participations_child ON public.experience_participations USING btree (child_id);


--
-- Name: idx_exp_participations_session; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_participations_session ON public.experience_participations USING btree (experience_session_id);


--
-- Name: experience_participations experience_participations_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_participations
    ADD CONSTRAINT experience_participations_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id);


--
-- Name: experience_participations experience_participations_experience_session_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_participations
    ADD CONSTRAINT experience_participations_experience_session_id_fkey FOREIGN KEY (experience_session_id) REFERENCES public.experience_sessions(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict Sl8yullChZBXLbOE4IjuRSthRnH7daTVJWIGhoxLCts8YylufElZ7TDRb2yk37g

