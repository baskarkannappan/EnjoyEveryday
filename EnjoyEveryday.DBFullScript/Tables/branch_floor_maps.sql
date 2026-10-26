--
-- PostgreSQL database dump
--

\restrict UEbXhJIdrheuqvWTMfyJl0AOZsnYIaMK6U9cP1SLCrMHeb9oaDludyzc1EZWgeU

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
-- Name: branch_floor_maps; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.branch_floor_maps (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    name character varying(255) NOT NULL,
    building_area character varying(255),
    floor_level character varying(100) NOT NULL,
    description text,
    file_storage_key character varying(500) NOT NULL,
    file_type character varying(50) NOT NULL,
    file_size bigint NOT NULL,
    content_hash character varying(128) NOT NULL,
    version integer DEFAULT 1 NOT NULL,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    review_due_at timestamp with time zone,
    created_by character varying(255) NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    published_by character varying(255),
    published_at timestamp with time zone,
    supersedes_map_id uuid,
    row_version integer DEFAULT 1 NOT NULL,
    emergency_instructions text
);


--
-- Name: branch_floor_maps branch_floor_maps_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_floor_maps
    ADD CONSTRAINT branch_floor_maps_pkey PRIMARY KEY (id);


--
-- Name: idx_branch_floor_maps_lookup; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_branch_floor_maps_lookup ON public.branch_floor_maps USING btree (tenant_id, organization_id, branch_id, status);


--
-- Name: branch_floor_maps branch_floor_maps_supersedes_map_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_floor_maps
    ADD CONSTRAINT branch_floor_maps_supersedes_map_id_fkey FOREIGN KEY (supersedes_map_id) REFERENCES public.branch_floor_maps(id);


--
-- PostgreSQL database dump complete
--

\unrestrict UEbXhJIdrheuqvWTMfyJl0AOZsnYIaMK6U9cP1SLCrMHeb9oaDludyzc1EZWgeU

