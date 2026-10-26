--
-- PostgreSQL database dump
--

\restrict GwpoK6q1BuzsAYsWK0gzaP3a1fMDnf779gFQelXIj4cIGywQ0cshi3S0z0tAo9e

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
-- Name: child_interests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_interests (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    name character varying(255) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: child_interests child_interests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_interests
    ADD CONSTRAINT child_interests_pkey PRIMARY KEY (id);


--
-- Name: idx_child_interests_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_interests_child ON public.child_interests USING btree (child_id);


--
-- Name: idx_child_interests_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_interests_tenant ON public.child_interests USING btree (tenant_id);


--
-- Name: child_interests child_interests_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_interests
    ADD CONSTRAINT child_interests_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict GwpoK6q1BuzsAYsWK0gzaP3a1fMDnf779gFQelXIj4cIGywQ0cshi3S0z0tAo9e

