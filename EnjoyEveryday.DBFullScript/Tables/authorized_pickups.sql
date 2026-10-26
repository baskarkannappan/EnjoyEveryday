--
-- PostgreSQL database dump
--

\restrict UaQN9T7a5tWRzxMywV669Vr4pTvD0aGJ0EBX7ZhSwDBdL2DzNtEq9iJz1grV7LB

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
-- Name: authorized_pickups; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.authorized_pickups (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    person_id uuid,
    relationship character varying(100),
    pickup_code character varying(50),
    photo_url text,
    valid_from timestamp with time zone,
    valid_to timestamp with time zone,
    is_active boolean DEFAULT true NOT NULL,
    notes text
);


--
-- Name: authorized_pickups authorized_pickups_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_pickups
    ADD CONSTRAINT authorized_pickups_pkey PRIMARY KEY (id);


--
-- Name: idx_authorized_pickups_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_authorized_pickups_child ON public.authorized_pickups USING btree (tenant_id, child_id, is_active);


--
-- Name: authorized_pickups authorized_pickups_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_pickups
    ADD CONSTRAINT authorized_pickups_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: authorized_pickups authorized_pickups_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_pickups
    ADD CONSTRAINT authorized_pickups_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.persons(id);


--
-- Name: authorized_pickups authorized_pickups_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_pickups
    ADD CONSTRAINT authorized_pickups_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict UaQN9T7a5tWRzxMywV669Vr4pTvD0aGJ0EBX7ZhSwDBdL2DzNtEq9iJz1grV7LB

