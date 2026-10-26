--
-- PostgreSQL database dump
--

\restrict IdRQuPbkAyxguU0D2swYpR3zdiIgeubC5KXhGaNFBIdMmADNUOwpiEwKm0ZPBgd

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
-- Name: family_relationships; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.family_relationships (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    user_id uuid NOT NULL,
    relationship character varying(50) NOT NULL,
    is_primary boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    status character varying(20) DEFAULT 'Pending'::character varying NOT NULL,
    verified_at timestamp with time zone,
    verified_by uuid,
    revoked_at timestamp with time zone,
    revoked_by uuid,
    CONSTRAINT chk_family_relationship_status CHECK (((status)::text = ANY ((ARRAY['Pending'::character varying, 'Verified'::character varying, 'Revoked'::character varying])::text[])))
);


--
-- Name: family_relationships family_relationships_child_id_user_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_child_id_user_id_key UNIQUE (child_id, user_id);


--
-- Name: family_relationships family_relationships_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_pkey PRIMARY KEY (id);


--
-- Name: idx_family_relationships_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_family_relationships_tenant ON public.family_relationships USING btree (tenant_id);


--
-- Name: idx_family_relationships_user_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_family_relationships_user_child ON public.family_relationships USING btree (tenant_id, user_id, child_id) WHERE (((status)::text = 'Verified'::text) AND (revoked_at IS NULL));


--
-- Name: family_relationships family_relationships_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: family_relationships family_relationships_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: family_relationships family_relationships_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict IdRQuPbkAyxguU0D2swYpR3zdiIgeubC5KXhGaNFBIdMmADNUOwpiEwKm0ZPBgd

