--
-- PostgreSQL database dump
--

\restrict TintCVpdCRB11YMH3nkCqEglBQuwXOIhXBuqcWWKEppNJ5fXu4vqt7cB6OzDocU

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
-- Name: material_orders; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_orders (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_number character varying(50) NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid,
    classroom_id uuid,
    planned_experience_id uuid,
    requesting_user_id uuid NOT NULL,
    requesting_user_email character varying(256),
    order_type character varying(50) DEFAULT 'StandardCatalog'::character varying NOT NULL,
    workflow_status character varying(50) DEFAULT 'RequestSubmitted'::character varying NOT NULL,
    payment_status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    fulfilment_status character varying(50) DEFAULT 'Unfulfilled'::character varying NOT NULL,
    total_purchase_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    total_rental_fee numeric(10,2) DEFAULT 0.00 NOT NULL,
    total_deposit numeric(10,2) DEFAULT 0.00 NOT NULL,
    shipping_fee numeric(10,2) DEFAULT 0.00 NOT NULL,
    tax_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    total_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    quotation_notes text,
    quotation_valid_until timestamp with time zone,
    rejection_reason text,
    cancellation_reason text,
    delivery_address text NOT NULL,
    delivery_contact_name character varying(150),
    delivery_contact_phone character varying(50),
    required_delivery_date timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_orders material_orders_order_number_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_order_number_key UNIQUE (order_number);


--
-- Name: material_orders material_orders_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_pkey PRIMARY KEY (id);


--
-- Name: idx_mat_orders_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_orders_exp ON public.material_orders USING btree (planned_experience_id);


--
-- Name: idx_mat_orders_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_orders_org ON public.material_orders USING btree (organization_id, created_at DESC);


--
-- Name: idx_mat_orders_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_orders_status ON public.material_orders USING btree (workflow_status);


--
-- Name: material_orders material_orders_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id) ON DELETE SET NULL;


--
-- Name: material_orders material_orders_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE SET NULL;


--
-- Name: material_orders material_orders_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: material_orders material_orders_planned_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_planned_experience_id_fkey FOREIGN KEY (planned_experience_id) REFERENCES public.experiences(id) ON DELETE SET NULL;


--
-- Name: material_orders material_orders_requesting_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_requesting_user_id_fkey FOREIGN KEY (requesting_user_id) REFERENCES public.users(id);


--
-- Name: material_orders material_orders_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict TintCVpdCRB11YMH3nkCqEglBQuwXOIhXBuqcWWKEppNJ5fXu4vqt7cB6OzDocU

