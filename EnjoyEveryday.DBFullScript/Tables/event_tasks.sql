--
-- PostgreSQL database dump
--

\restrict cnzInvTWH7dPbvBdp8jp15ftRrxYixuJsojoDHOdHCpghOKO9W9Pkh4JsOE9XRu

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
-- Name: event_tasks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.event_tasks (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    event_id uuid NOT NULL,
    description text NOT NULL,
    owner_id uuid,
    due_date timestamp with time zone,
    is_completed boolean DEFAULT false NOT NULL,
    completed_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: event_tasks event_tasks_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_tasks
    ADD CONSTRAINT event_tasks_pkey PRIMARY KEY (id);


--
-- Name: idx_event_tasks_event; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_event_tasks_event ON public.event_tasks USING btree (event_id);


--
-- Name: event_tasks event_tasks_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_tasks
    ADD CONSTRAINT event_tasks_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE CASCADE;


--
-- Name: event_tasks event_tasks_owner_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_tasks
    ADD CONSTRAINT event_tasks_owner_id_fkey FOREIGN KEY (owner_id) REFERENCES public.users(id);


--
-- PostgreSQL database dump complete
--

\unrestrict cnzInvTWH7dPbvBdp8jp15ftRrxYixuJsojoDHOdHCpghOKO9W9Pkh4JsOE9XRu

