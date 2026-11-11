--
-- PostgreSQL database dump
--

\restrict uLnzd1DxCT2CRpleUDsdFL0sEbl8CCqVoCbMRjwWFnDNVLSq847kx63dSjdu9mF

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
-- Name: daycare_announcements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.daycare_announcements (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid,
    title character varying(255) NOT NULL,
    content text NOT NULL,
    tag character varying(50) DEFAULT 'Notice'::character varying NOT NULL,
    is_urgent boolean DEFAULT false NOT NULL,
    author_name character varying(150),
    author_role character varying(50),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: daycare_announcements daycare_announcements_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.daycare_announcements
    ADD CONSTRAINT daycare_announcements_pkey PRIMARY KEY (id);


--
-- Name: idx_daycare_announcements_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_daycare_announcements_org ON public.daycare_announcements USING btree (tenant_id, organization_id, created_at DESC);


--
-- PostgreSQL database dump complete
--

\unrestrict uLnzd1DxCT2CRpleUDsdFL0sEbl8CCqVoCbMRjwWFnDNVLSq847kx63dSjdu9mF

