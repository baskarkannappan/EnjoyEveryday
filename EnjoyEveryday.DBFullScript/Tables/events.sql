--
-- PostgreSQL database dump
--

\restrict mnbhDee6v3NDzjS0GFAWBoQpy2QRUehoGy2WffoOOQAEa90IRnDoSB8DhNtmc0d

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
-- Name: events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    title character varying(200) NOT NULL,
    description text,
    event_type_id uuid,
    event_date date NOT NULL,
    start_time time without time zone,
    end_time time without time zone,
    location character varying(255),
    organizer_id uuid,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    is_organization_wide boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    scope character varying(30) DEFAULT 'Classroom'::character varying NOT NULL,
    branch_id uuid,
    photo_urls text[] DEFAULT '{}'::text[]
);


--
-- Name: events events_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_pkey PRIMARY KEY (id);


--
-- Name: idx_events_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_events_branch ON public.events USING btree (branch_id);


--
-- Name: idx_events_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_events_date ON public.events USING btree (event_date);


--
-- Name: idx_events_scope; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_events_scope ON public.events USING btree (tenant_id, scope);


--
-- Name: idx_events_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_events_tenant ON public.events USING btree (tenant_id);


--
-- Name: events events_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id) ON DELETE SET NULL;


--
-- Name: events events_event_type_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_event_type_id_fkey FOREIGN KEY (event_type_id) REFERENCES public.event_types(id);


--
-- Name: events events_organizer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_organizer_id_fkey FOREIGN KEY (organizer_id) REFERENCES public.users(id);


--
-- Name: events events_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict mnbhDee6v3NDzjS0GFAWBoQpy2QRUehoGy2WffoOOQAEa90IRnDoSB8DhNtmc0d

