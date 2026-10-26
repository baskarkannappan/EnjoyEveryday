--
-- PostgreSQL database dump
--

\restrict eBdfdOSarmCKBubsZgsklvngYX1Qt4CGeMrQjeXbvQntQaRIQdAuYxDhCoTbYpP

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
-- Name: material_shipments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_shipments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    shipment_number character varying(50) NOT NULL,
    carrier_name character varying(100) NOT NULL,
    tracking_reference character varying(100) NOT NULL,
    dispatch_date timestamp with time zone DEFAULT now() NOT NULL,
    estimated_delivery_date timestamp with time zone,
    actual_delivery_date timestamp with time zone,
    delivery_status character varying(50) DEFAULT 'Dispatched'::character varying NOT NULL,
    proof_of_delivery_notes text,
    recorded_by_user_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_shipments material_shipments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_shipments
    ADD CONSTRAINT material_shipments_pkey PRIMARY KEY (id);


--
-- Name: material_shipments material_shipments_shipment_number_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_shipments
    ADD CONSTRAINT material_shipments_shipment_number_key UNIQUE (shipment_number);


--
-- Name: idx_mat_shipments_order; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_shipments_order ON public.material_shipments USING btree (order_id);


--
-- Name: material_shipments material_shipments_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_shipments
    ADD CONSTRAINT material_shipments_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id) ON DELETE CASCADE;


--
-- Name: material_shipments material_shipments_recorded_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_shipments
    ADD CONSTRAINT material_shipments_recorded_by_user_id_fkey FOREIGN KEY (recorded_by_user_id) REFERENCES public.users(id);


--
-- PostgreSQL database dump complete
--

\unrestrict eBdfdOSarmCKBubsZgsklvngYX1Qt4CGeMrQjeXbvQntQaRIQdAuYxDhCoTbYpP

