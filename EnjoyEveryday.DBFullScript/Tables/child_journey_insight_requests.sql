--
-- PostgreSQL database dump
--

\restrict xcu6Ff18rtabNd3WJyOe51tuWcGLxzXtQJd88a6fEnYZCKgWeCCykZvjAhTVSRz

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
-- Name: child_journey_insight_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_insight_requests (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    child_id uuid NOT NULL,
    requested_by_user_id uuid NOT NULL,
    period_type character varying(50) DEFAULT 'Last30Days'::character varying NOT NULL,
    period_start_date date NOT NULL,
    period_end_date date NOT NULL,
    evidence_count integer DEFAULT 0 NOT NULL,
    overall_summary text,
    evidence_gaps text,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    focus_area character varying(100),
    teacher_question text
);


--
-- Name: child_journey_insight_requests child_journey_insight_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_insight_requests
    ADD CONSTRAINT child_journey_insight_requests_pkey PRIMARY KEY (id);


--
-- Name: idx_journey_insight_requests_tenant_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_insight_requests_tenant_child ON public.child_journey_insight_requests USING btree (tenant_id, child_id, created_at DESC);


--
-- Name: child_journey_insight_requests child_journey_insight_requests_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_insight_requests
    ADD CONSTRAINT child_journey_insight_requests_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict xcu6Ff18rtabNd3WJyOe51tuWcGLxzXtQJd88a6fEnYZCKgWeCCykZvjAhTVSRz

