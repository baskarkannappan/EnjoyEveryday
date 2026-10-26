--
-- PostgreSQL database dump
--

\restrict cQv6yeR8TYr5e9AtGWumPb58kEf55syfJrSyOOpIY8JxTkUbEKhuWQSY8jV6dlY

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
-- Name: floor_map_markers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.floor_map_markers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    floor_map_id uuid NOT NULL,
    marker_type character varying(100) NOT NULL,
    label character varying(255) NOT NULL,
    instructions text,
    normalized_x double precision NOT NULL,
    normalized_y double precision NOT NULL,
    display_order integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: floor_map_markers floor_map_markers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.floor_map_markers
    ADD CONSTRAINT floor_map_markers_pkey PRIMARY KEY (id);


--
-- Name: idx_floor_map_markers_map_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_floor_map_markers_map_id ON public.floor_map_markers USING btree (floor_map_id);


--
-- Name: floor_map_markers floor_map_markers_floor_map_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.floor_map_markers
    ADD CONSTRAINT floor_map_markers_floor_map_id_fkey FOREIGN KEY (floor_map_id) REFERENCES public.branch_floor_maps(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict cQv6yeR8TYr5e9AtGWumPb58kEf55syfJrSyOOpIY8JxTkUbEKhuWQSY8jV6dlY

