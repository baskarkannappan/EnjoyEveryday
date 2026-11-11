--
-- PostgreSQL database dump
--

\restrict xNpf0r2249hjMkRL9ahUjH6UYYDbHHspPjSazLdrENN8mdzh5Dbn3z5RoMlkp5x

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
-- Name: master_lookups; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.master_lookups (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    category character varying(50) NOT NULL,
    code character varying(50) NOT NULL,
    display_name character varying(100) NOT NULL,
    display_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    metadata_json jsonb DEFAULT '{}'::jsonb,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: master_lookups master_lookups_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.master_lookups
    ADD CONSTRAINT master_lookups_pkey PRIMARY KEY (id);


--
-- Name: master_lookups uq_master_lookups_category_code; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.master_lookups
    ADD CONSTRAINT uq_master_lookups_category_code UNIQUE (category, code);


--
-- Name: idx_master_lookups_category; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_master_lookups_category ON public.master_lookups USING btree (category, is_active, display_order);


--
-- PostgreSQL database dump complete
--

\unrestrict xNpf0r2249hjMkRL9ahUjH6UYYDbHHspPjSazLdrENN8mdzh5Dbn3z5RoMlkp5x

