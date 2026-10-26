--
-- PostgreSQL database dump
--

\restrict t2dTbc9peEvhwVg4GFggOLLj0SYsdsgtgFOWfp4g0Wt8W75hrH53nGEQd5G065R

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
-- Name: child_transportations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_transportations (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    transportation_required boolean DEFAULT false NOT NULL,
    pickup_location character varying(255),
    dropoff_location character varying(255),
    route character varying(100),
    vehicle character varying(100),
    authorized_adult character varying(255),
    special_instructions text
);


--
-- Name: child_transportations child_transportations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_transportations
    ADD CONSTRAINT child_transportations_pkey PRIMARY KEY (child_id);


--
-- Name: child_transportations child_transportations_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_transportations
    ADD CONSTRAINT child_transportations_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_transportations child_transportations_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_transportations
    ADD CONSTRAINT child_transportations_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict t2dTbc9peEvhwVg4GFggOLLj0SYsdsgtgFOWfp4g0Wt8W75hrH53nGEQd5G065R

