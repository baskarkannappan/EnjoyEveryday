--
-- PostgreSQL database dump
--

\restrict Mr4r724Q6XnDFYrrQTUQrN9bUKhHmQJCB0g3WhTiz4YSxu01D5aVf9kqfAtZ1os

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
-- Name: daily_readiness_checklist_logs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.daily_readiness_checklist_logs (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid,
    classroom_id uuid,
    check_date date NOT NULL,
    item_id character varying(100) NOT NULL,
    is_completed boolean NOT NULL,
    updated_by_user_id uuid,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: daily_readiness_checklist_logs daily_readiness_checklist_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.daily_readiness_checklist_logs
    ADD CONSTRAINT daily_readiness_checklist_logs_pkey PRIMARY KEY (id);


--
-- Name: daily_readiness_checklist_logs uq_daily_readiness_item; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.daily_readiness_checklist_logs
    ADD CONSTRAINT uq_daily_readiness_item UNIQUE (tenant_id, organization_id, check_date, item_id);


--
-- Name: idx_readiness_logs_lookup; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_readiness_logs_lookup ON public.daily_readiness_checklist_logs USING btree (tenant_id, organization_id, check_date);


--
-- PostgreSQL database dump complete
--

\unrestrict Mr4r724Q6XnDFYrrQTUQrN9bUKhHmQJCB0g3WhTiz4YSxu01D5aVf9kqfAtZ1os

