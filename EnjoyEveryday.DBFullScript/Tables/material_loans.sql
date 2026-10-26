--
-- PostgreSQL database dump
--

\restrict zpdlIhhkheliURvM4z1xiJ72tqe7ElagPdOnx6aUxwoYTyAy9WMtF0rCFGenFqm

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
-- Name: material_loans; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_loans (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    order_item_id uuid NOT NULL,
    material_product_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid,
    quantity_issued integer DEFAULT 1 NOT NULL,
    issued_at timestamp with time zone DEFAULT now() NOT NULL,
    due_date timestamp with time zone NOT NULL,
    actual_return_date timestamp with time zone,
    return_status character varying(50) DEFAULT 'Reserved'::character varying NOT NULL,
    deposit_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    deposit_status character varying(50) DEFAULT 'Held'::character varying NOT NULL,
    notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_loans material_loans_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_pkey PRIMARY KEY (id);


--
-- Name: idx_mat_loans_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_loans_org ON public.material_loans USING btree (organization_id, due_date);


--
-- Name: idx_mat_loans_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_loans_status ON public.material_loans USING btree (return_status);


--
-- Name: material_loans material_loans_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: material_loans material_loans_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id);


--
-- Name: material_loans material_loans_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id) ON DELETE CASCADE;


--
-- Name: material_loans material_loans_order_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_order_item_id_fkey FOREIGN KEY (order_item_id) REFERENCES public.material_order_items(id) ON DELETE CASCADE;


--
-- Name: material_loans material_loans_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- PostgreSQL database dump complete
--

\unrestrict zpdlIhhkheliURvM4z1xiJ72tqe7ElagPdOnx6aUxwoYTyAy9WMtF0rCFGenFqm

