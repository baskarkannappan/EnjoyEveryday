--
-- PostgreSQL database dump
--

\restrict mAIJdIO5r6LFXd9ozAtV4Za4TGuggjANVWhLOZhOEzat6fBoJ1l44SqrVF3LYSR

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
-- Name: child_relationships; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_relationships (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    related_child_id uuid NOT NULL,
    relationship_type character varying(100) NOT NULL,
    notes text
);


--
-- Name: child_relationships child_relationships_child_id_related_child_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_child_id_related_child_id_key UNIQUE (child_id, related_child_id);


--
-- Name: child_relationships child_relationships_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_pkey PRIMARY KEY (id);


--
-- Name: child_relationships child_relationships_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_relationships child_relationships_related_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_related_child_id_fkey FOREIGN KEY (related_child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_relationships child_relationships_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict mAIJdIO5r6LFXd9ozAtV4Za4TGuggjANVWhLOZhOEzat6fBoJ1l44SqrVF3LYSR

