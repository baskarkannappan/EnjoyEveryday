--
-- PostgreSQL database dump
--

\restrict hO1g9RHPHJ08kKaL0r0W9m5hejYsLTpu35uz2uBlyGhbyBOLh6AoL7e33D5QsSo

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
-- Name: child_journey_evidence; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_evidence (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    journey_entry_id uuid NOT NULL,
    evidence_type character varying(100) NOT NULL,
    media_id uuid,
    text_content text,
    captured_at timestamp with time zone,
    captured_by uuid,
    visibility character varying(50) DEFAULT 'InternalOnly'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: child_journey_evidence child_journey_evidence_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_evidence
    ADD CONSTRAINT child_journey_evidence_pkey PRIMARY KEY (id);


--
-- Name: idx_journey_evidence_journey; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_evidence_journey ON public.child_journey_evidence USING btree (journey_entry_id);


--
-- Name: child_journey_evidence child_journey_evidence_journey_entry_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_evidence
    ADD CONSTRAINT child_journey_evidence_journey_entry_id_fkey FOREIGN KEY (journey_entry_id) REFERENCES public.child_journey_entries(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict hO1g9RHPHJ08kKaL0r0W9m5hejYsLTpu35uz2uBlyGhbyBOLh6AoL7e33D5QsSo

