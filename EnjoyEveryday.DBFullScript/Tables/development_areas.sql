--
-- PostgreSQL database dump
--

\restrict hrubmlGDxezIqI0ae6sJ2n2YT7cXxIQtxRTdlrEfW0GgUVuoeB6lY7aSYG9cXXv

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
-- Name: development_areas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.development_areas (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid,
    name character varying(255) NOT NULL,
    description text,
    icon character varying(100),
    display_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: development_areas development_areas_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.development_areas
    ADD CONSTRAINT development_areas_pkey PRIMARY KEY (id);


--
-- Name: idx_development_areas_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_development_areas_tenant ON public.development_areas USING btree (tenant_id);


--
-- Name: development_areas development_areas_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.development_areas
    ADD CONSTRAINT development_areas_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict hrubmlGDxezIqI0ae6sJ2n2YT7cXxIQtxRTdlrEfW0GgUVuoeB6lY7aSYG9cXXv

