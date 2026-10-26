--
-- PostgreSQL database dump
--

\restrict LlOm8ZDOzh68pK213sd2prYzMMDWkyWS6sTWjbxFIfz3aNeYZIPWJdklHvDJ6bm

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
-- Name: material_return_inspections; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_return_inspections (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    loan_id uuid NOT NULL,
    order_id uuid NOT NULL,
    material_product_id uuid NOT NULL,
    returned_quantity integer NOT NULL,
    inspected_at timestamp with time zone DEFAULT now() NOT NULL,
    inspected_by_user_id uuid,
    inspected_by_email character varying(256),
    condition character varying(50) DEFAULT 'Good'::character varying NOT NULL,
    quantity_approved_for_stock integer DEFAULT 0 NOT NULL,
    quantity_requiring_cleaning integer DEFAULT 0 NOT NULL,
    quantity_damaged integer DEFAULT 0 NOT NULL,
    quantity_missing integer DEFAULT 0 NOT NULL,
    inspection_notes text,
    deposit_disposition character varying(50) DEFAULT 'FullRefund'::character varying NOT NULL,
    deposit_deduction_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    resolution_status character varying(50) DEFAULT 'Completed'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_return_inspections material_return_inspections_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_pkey PRIMARY KEY (id);


--
-- Name: idx_mat_inspections_loan; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_inspections_loan ON public.material_return_inspections USING btree (loan_id);


--
-- Name: material_return_inspections material_return_inspections_inspected_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_inspected_by_user_id_fkey FOREIGN KEY (inspected_by_user_id) REFERENCES public.users(id);


--
-- Name: material_return_inspections material_return_inspections_loan_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_loan_id_fkey FOREIGN KEY (loan_id) REFERENCES public.material_loans(id) ON DELETE CASCADE;


--
-- Name: material_return_inspections material_return_inspections_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id);


--
-- Name: material_return_inspections material_return_inspections_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id);


--
-- PostgreSQL database dump complete
--

\unrestrict LlOm8ZDOzh68pK213sd2prYzMMDWkyWS6sTWjbxFIfz3aNeYZIPWJdklHvDJ6bm

