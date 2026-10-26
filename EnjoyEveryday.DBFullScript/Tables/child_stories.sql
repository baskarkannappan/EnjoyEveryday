--
-- PostgreSQL database dump
--

\restrict jPtzaN2Yy9S0ywZmOiGy4wRnguxL5SzHsMZ2IQCk8wpsCAv7sH9qS2Oth5lRv2v

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
-- Name: child_stories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_stories (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    experience_schedule_id uuid,
    content text NOT NULL,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: child_stories child_stories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_stories
    ADD CONSTRAINT child_stories_pkey PRIMARY KEY (id);


--
-- Name: idx_child_stories_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_stories_child ON public.child_stories USING btree (child_id);


--
-- Name: idx_child_stories_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_stories_tenant ON public.child_stories USING btree (tenant_id);


--
-- Name: child_stories child_stories_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_stories
    ADD CONSTRAINT child_stories_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_stories child_stories_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_stories
    ADD CONSTRAINT child_stories_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- PostgreSQL database dump complete
--

\unrestrict jPtzaN2Yy9S0ywZmOiGy4wRnguxL5SzHsMZ2IQCk8wpsCAv7sH9qS2Oth5lRv2v

