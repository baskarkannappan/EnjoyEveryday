--
-- PostgreSQL database dump
--

\restrict dREfLxDuP3cRFIyCMjEtZsxFIixXusHR8XZgecqsHfDShqzAnmPemRMLPnOjwET

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
-- Name: activity_recommendation_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_recommendation_items (
    id uuid NOT NULL,
    request_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    experience_title character varying(255) NOT NULL,
    suggested_format character varying(100),
    estimated_duration_minutes integer,
    rationale text NOT NULL,
    supporting_evidence jsonb,
    materials jsonb,
    rank_order integer DEFAULT 1 NOT NULL,
    is_evidence_limited boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: activity_recommendation_items activity_recommendation_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_items
    ADD CONSTRAINT activity_recommendation_items_pkey PRIMARY KEY (id);


--
-- Name: idx_rec_items_request; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_rec_items_request ON public.activity_recommendation_items USING btree (request_id);


--
-- Name: activity_recommendation_items activity_recommendation_items_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_items
    ADD CONSTRAINT activity_recommendation_items_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: activity_recommendation_items activity_recommendation_items_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_items
    ADD CONSTRAINT activity_recommendation_items_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.activity_recommendation_requests(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict dREfLxDuP3cRFIyCMjEtZsxFIixXusHR8XZgecqsHfDShqzAnmPemRMLPnOjwET

