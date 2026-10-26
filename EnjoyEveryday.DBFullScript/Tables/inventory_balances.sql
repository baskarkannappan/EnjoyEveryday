--
-- PostgreSQL database dump
--

\restrict 9efoRd3SrDnyWBdY6HgLRiDK60b2HnTfuNT46q8D1jhmRZCPcqogV346W4vhwXv

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
-- Name: inventory_balances; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory_balances (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    material_product_id uuid NOT NULL,
    location_id uuid NOT NULL,
    total_stock integer DEFAULT 0 NOT NULL,
    available_stock integer DEFAULT 0 NOT NULL,
    reserved_stock integer DEFAULT 0 NOT NULL,
    dispatched_stock integer DEFAULT 0 NOT NULL,
    on_loan_stock integer DEFAULT 0 NOT NULL,
    under_inspection_stock integer DEFAULT 0 NOT NULL,
    under_cleaning_stock integer DEFAULT 0 NOT NULL,
    damaged_stock integer DEFAULT 0 NOT NULL,
    missing_stock integer DEFAULT 0 NOT NULL,
    retired_stock integer DEFAULT 0 NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT chk_avail_nonneg CHECK ((available_stock >= 0)),
    CONSTRAINT chk_loan_nonneg CHECK ((on_loan_stock >= 0)),
    CONSTRAINT chk_reserv_nonneg CHECK ((reserved_stock >= 0))
);


--
-- Name: inventory_balances inventory_balances_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_balances
    ADD CONSTRAINT inventory_balances_pkey PRIMARY KEY (id);


--
-- Name: inventory_balances uk_inv_bal_prod_loc; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_balances
    ADD CONSTRAINT uk_inv_bal_prod_loc UNIQUE (material_product_id, location_id);


--
-- Name: idx_inv_bal_prod; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inv_bal_prod ON public.inventory_balances USING btree (material_product_id);


--
-- Name: inventory_balances inventory_balances_location_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_balances
    ADD CONSTRAINT inventory_balances_location_id_fkey FOREIGN KEY (location_id) REFERENCES public.inventory_locations(id) ON DELETE CASCADE;


--
-- Name: inventory_balances inventory_balances_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_balances
    ADD CONSTRAINT inventory_balances_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 9efoRd3SrDnyWBdY6HgLRiDK60b2HnTfuNT46q8D1jhmRZCPcqogV346W4vhwXv

