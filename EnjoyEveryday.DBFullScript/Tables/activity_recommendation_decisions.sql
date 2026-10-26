--
-- PostgreSQL database dump
--

\restrict KCJYaGnflfE76DDeVb7Ev0Od9wPldy04ln6LBxv9OhKKDqqq3GJcSPlHDPXv63E

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
-- Name: activity_recommendation_decisions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_recommendation_decisions (
    id uuid NOT NULL,
    request_id uuid NOT NULL,
    recommendation_item_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    decided_by_user_id uuid NOT NULL,
    decision character varying(50) NOT NULL,
    rejection_reason text,
    resulting_schedule_id uuid,
    decided_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: activity_recommendation_decisions activity_recommendation_decisions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_decisions
    ADD CONSTRAINT activity_recommendation_decisions_pkey PRIMARY KEY (id);


--
-- Name: idx_rec_decisions_req; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_rec_decisions_req ON public.activity_recommendation_decisions USING btree (request_id);


--
-- Name: activity_recommendation_decisions activity_recommendation_decisions_recommendation_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_decisions
    ADD CONSTRAINT activity_recommendation_decisions_recommendation_item_id_fkey FOREIGN KEY (recommendation_item_id) REFERENCES public.activity_recommendation_items(id) ON DELETE CASCADE;


--
-- Name: activity_recommendation_decisions activity_recommendation_decisions_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_decisions
    ADD CONSTRAINT activity_recommendation_decisions_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.activity_recommendation_requests(id) ON DELETE CASCADE;


--
-- Name: activity_recommendation_decisions activity_recommendation_decisions_resulting_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_decisions
    ADD CONSTRAINT activity_recommendation_decisions_resulting_schedule_id_fkey FOREIGN KEY (resulting_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- PostgreSQL database dump complete
--

\unrestrict KCJYaGnflfE76DDeVb7Ev0Od9wPldy04ln6LBxv9OhKKDqqq3GJcSPlHDPXv63E

