--
-- PostgreSQL database dump
--

\restrict BOL7314tWDO2ANZhJXgDhwDxxnfJexa77aq5k1wZalaEFL3bEeIiSoK91vl9MSF

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
-- Name: event_rsvps; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.event_rsvps (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    event_id uuid NOT NULL,
    child_id uuid NOT NULL,
    guardian_id uuid NOT NULL,
    status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: event_rsvps event_rsvps_event_id_child_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_event_id_child_id_key UNIQUE (event_id, child_id);


--
-- Name: event_rsvps event_rsvps_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_pkey PRIMARY KEY (id);


--
-- Name: idx_event_rsvps_event; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_event_rsvps_event ON public.event_rsvps USING btree (event_id);


--
-- Name: event_rsvps event_rsvps_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id);


--
-- Name: event_rsvps event_rsvps_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE CASCADE;


--
-- Name: event_rsvps event_rsvps_guardian_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_guardian_id_fkey FOREIGN KEY (guardian_id) REFERENCES public.users(id);


--
-- PostgreSQL database dump complete
--

\unrestrict BOL7314tWDO2ANZhJXgDhwDxxnfJexa77aq5k1wZalaEFL3bEeIiSoK91vl9MSF

