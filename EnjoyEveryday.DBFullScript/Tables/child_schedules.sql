--
-- PostgreSQL database dump
--

\restrict PW8uOjThDPjMMaVyqaJORCFsb4YJe8KgoerloPv6kLIYSoyP1R40NSAp6SbhnGU

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
-- Name: child_schedules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_schedules (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    day_of_week integer NOT NULL,
    expected_arrival character varying(50),
    expected_departure character varying(50)
);


--
-- Name: child_schedules child_schedules_child_id_day_of_week_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_schedules
    ADD CONSTRAINT child_schedules_child_id_day_of_week_key UNIQUE (child_id, day_of_week);


--
-- Name: child_schedules child_schedules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_schedules
    ADD CONSTRAINT child_schedules_pkey PRIMARY KEY (id);


--
-- Name: child_schedules child_schedules_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_schedules
    ADD CONSTRAINT child_schedules_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_schedules child_schedules_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_schedules
    ADD CONSTRAINT child_schedules_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict PW8uOjThDPjMMaVyqaJORCFsb4YJe8KgoerloPv6kLIYSoyP1R40NSAp6SbhnGU

