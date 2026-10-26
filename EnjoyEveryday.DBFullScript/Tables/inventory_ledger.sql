--
-- PostgreSQL database dump
--

\restrict dapmzrG4M6fobQBsRUID3vPc8SxMpTYEIsOeylTC5YFTU7FARFuR2KDrPBgVLqy

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
-- Name: inventory_ledger; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory_ledger (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    material_product_id uuid NOT NULL,
    location_id uuid NOT NULL,
    transaction_type character varying(50) NOT NULL,
    quantity_delta integer DEFAULT 0 NOT NULL,
    available_delta integer DEFAULT 0 NOT NULL,
    reserved_delta integer DEFAULT 0 NOT NULL,
    on_loan_delta integer DEFAULT 0 NOT NULL,
    reference_type character varying(50),
    reference_id uuid,
    notes text,
    performed_by_user_id uuid,
    performed_by_email character varying(256),
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: inventory_ledger inventory_ledger_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_ledger
    ADD CONSTRAINT inventory_ledger_pkey PRIMARY KEY (id);


--
-- Name: idx_inv_ledger_prod; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inv_ledger_prod ON public.inventory_ledger USING btree (material_product_id, created_at DESC);


--
-- Name: idx_inv_ledger_ref; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inv_ledger_ref ON public.inventory_ledger USING btree (reference_type, reference_id);


--
-- Name: inventory_ledger inventory_ledger_location_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_ledger
    ADD CONSTRAINT inventory_ledger_location_id_fkey FOREIGN KEY (location_id) REFERENCES public.inventory_locations(id) ON DELETE CASCADE;


--
-- Name: inventory_ledger inventory_ledger_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_ledger
    ADD CONSTRAINT inventory_ledger_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict dapmzrG4M6fobQBsRUID3vPc8SxMpTYEIsOeylTC5YFTU7FARFuR2KDrPBgVLqy

