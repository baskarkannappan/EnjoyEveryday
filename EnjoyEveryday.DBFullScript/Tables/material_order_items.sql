--
-- PostgreSQL database dump
--

\restrict yt51MjB0M0OvkXO6Kfv7anjSoktw3PdOhU0AKy7RvEPXlTH36tgcewj0639yF4G

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
-- Name: material_order_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_order_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    material_product_id uuid NOT NULL,
    offering_id uuid,
    fulfilment_mode character varying(50) DEFAULT 'Purchase'::character varying NOT NULL,
    quantity integer DEFAULT 1 NOT NULL,
    unit_price numeric(10,2) DEFAULT 0.00 NOT NULL,
    rental_fee numeric(10,2) DEFAULT 0.00 NOT NULL,
    deposit numeric(10,2) DEFAULT 0.00 NOT NULL,
    line_total numeric(10,2) DEFAULT 0.00 NOT NULL,
    loan_duration_days integer DEFAULT 0 NOT NULL,
    return_due_date timestamp with time zone,
    status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_order_items material_order_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_items
    ADD CONSTRAINT material_order_items_pkey PRIMARY KEY (id);


--
-- Name: idx_mat_order_items_order; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_order_items_order ON public.material_order_items USING btree (order_id);


--
-- Name: material_order_items material_order_items_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_items
    ADD CONSTRAINT material_order_items_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id);


--
-- Name: material_order_items material_order_items_offering_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_items
    ADD CONSTRAINT material_order_items_offering_id_fkey FOREIGN KEY (offering_id) REFERENCES public.material_offerings(id);


--
-- Name: material_order_items material_order_items_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_items
    ADD CONSTRAINT material_order_items_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict yt51MjB0M0OvkXO6Kfv7anjSoktw3PdOhU0AKy7RvEPXlTH36tgcewj0639yF4G

