--
-- PostgreSQL database dump
--

\restrict mk0pFtLraPl8hitEnMExIwOEVlMyCwHzwQRCbc541HtdBheHHfWWUCr3z8oOrbO

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
-- Name: child_journey_insight_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_insight_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    request_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    category character varying(50) NOT NULL,
    classification character varying(50) DEFAULT 'RecordedFact'::character varying NOT NULL,
    statement text NOT NULL,
    evidence_strength character varying(50) DEFAULT 'Supported'::character varying NOT NULL,
    supporting_evidence jsonb,
    suggested_experience_id uuid,
    suggested_experience_title character varying(255),
    rank_order integer DEFAULT 1 NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_journey_insight_items child_journey_insight_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_insight_items
    ADD CONSTRAINT child_journey_insight_items_pkey PRIMARY KEY (id);


--
-- Name: idx_journey_insight_items_request; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_insight_items_request ON public.child_journey_insight_items USING btree (request_id, category, rank_order);


--
-- Name: child_journey_insight_items child_journey_insight_items_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_insight_items
    ADD CONSTRAINT child_journey_insight_items_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.child_journey_insight_requests(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict mk0pFtLraPl8hitEnMExIwOEVlMyCwHzwQRCbc541HtdBheHHfWWUCr3z8oOrbO

