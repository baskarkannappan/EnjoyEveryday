--
-- PostgreSQL database dump
--

\restrict YLYTFLHdf6nzSynM1KTM9KAe46mGevabm5cS15gdnGri3zD07veCoTDJO8HbknD

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
-- Name: media_assets; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.media_assets (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid,
    child_id uuid,
    entity_type character varying(50) NOT NULL,
    entity_id uuid,
    display_container character varying(100) NOT NULL,
    display_key character varying(255) NOT NULL,
    display_content_type character varying(100) NOT NULL,
    display_size_bytes bigint NOT NULL,
    display_width integer NOT NULL,
    display_height integer NOT NULL,
    thumbnail_container character varying(100) NOT NULL,
    thumbnail_key character varying(255) NOT NULL,
    thumbnail_content_type character varying(100) NOT NULL,
    thumbnail_size_bytes bigint NOT NULL,
    thumbnail_width integer NOT NULL,
    thumbnail_height integer NOT NULL,
    content_hash character varying(128) NOT NULL,
    created_by uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: media_assets media_assets_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.media_assets
    ADD CONSTRAINT media_assets_pkey PRIMARY KEY (id);


--
-- Name: idx_media_assets_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_media_assets_child ON public.media_assets USING btree (child_id);


--
-- Name: idx_media_assets_entity; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_media_assets_entity ON public.media_assets USING btree (entity_type, entity_id);


--
-- Name: idx_media_assets_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_media_assets_tenant ON public.media_assets USING btree (tenant_id);


--
-- Name: media_assets media_assets_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.media_assets
    ADD CONSTRAINT media_assets_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: media_assets media_assets_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.media_assets
    ADD CONSTRAINT media_assets_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE SET NULL;


--
-- Name: media_assets media_assets_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.media_assets
    ADD CONSTRAINT media_assets_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict YLYTFLHdf6nzSynM1KTM9KAe46mGevabm5cS15gdnGri3zD07veCoTDJO8HbknD

