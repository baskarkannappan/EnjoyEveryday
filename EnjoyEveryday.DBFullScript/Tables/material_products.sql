--
-- PostgreSQL database dump
--

\restrict 7QGXQBxidzsXaEAA4SOS93Lv9i2GnpUYsRXKB6qjfC6uYUWlAULa0BUrY00F7aR

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
-- Name: material_products; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_products (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    sku character varying(64) NOT NULL,
    name character varying(200) NOT NULL,
    description text NOT NULL,
    category character varying(100) DEFAULT 'General'::character varying NOT NULL,
    image_url character varying(500),
    material_type character varying(50) DEFAULT 'Consumable'::character varying NOT NULL,
    safety_instructions text,
    age_min_months integer,
    age_max_months integer,
    usage_restrictions text,
    cleaning_instructions text,
    return_policy text,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_products material_products_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_products
    ADD CONSTRAINT material_products_pkey PRIMARY KEY (id);


--
-- Name: material_products material_products_sku_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_products
    ADD CONSTRAINT material_products_sku_key UNIQUE (sku);


--
-- Name: idx_mat_prod_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_prod_active ON public.material_products USING btree (is_active);


--
-- Name: idx_mat_prod_cat; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_prod_cat ON public.material_products USING btree (category);


--
-- Name: idx_mat_prod_type; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_prod_type ON public.material_products USING btree (material_type);


--
-- PostgreSQL database dump complete
--

\unrestrict 7QGXQBxidzsXaEAA4SOS93Lv9i2GnpUYsRXKB6qjfC6uYUWlAULa0BUrY00F7aR

