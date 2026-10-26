--
-- PostgreSQL database dump
--

\restrict GN5R3N3vPvGAmTJZPAS6Ffg2hTOANogYxBLdepLmuA8BdgFsZPYbBgjdZMsU9nN

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
-- Name: activity_recommendation_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_recommendation_requests (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    child_id uuid,
    requested_by_user_id uuid NOT NULL,
    request_timestamp timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    scope character varying(50) NOT NULL,
    context_preferences jsonb,
    evidence_count integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: activity_recommendation_requests activity_recommendation_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_requests
    ADD CONSTRAINT activity_recommendation_requests_pkey PRIMARY KEY (id);


--
-- Name: idx_rec_req_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_rec_req_child ON public.activity_recommendation_requests USING btree (tenant_id, child_id);


--
-- Name: idx_rec_req_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_rec_req_classroom ON public.activity_recommendation_requests USING btree (tenant_id, classroom_id);


--
-- PostgreSQL database dump complete
--

\unrestrict GN5R3N3vPvGAmTJZPAS6Ffg2hTOANogYxBLdepLmuA8BdgFsZPYbBgjdZMsU9nN

