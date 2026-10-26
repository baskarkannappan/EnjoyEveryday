--
-- PostgreSQL database dump
--

\restrict eHblnhaKdze43sQrtVXnuRGKWoAPc8wKoVOHLYLXgf2dKtNJP2Qw64xNkP5CFLZ

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
-- Name: material_recurring_schedules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_recurring_schedules (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid,
    classroom_id uuid,
    schedule_type character varying(50) DEFAULT 'RecurringPurchase'::character varying NOT NULL,
    frequency character varying(50) DEFAULT 'Monthly'::character varying NOT NULL,
    material_product_id uuid NOT NULL,
    offering_id uuid,
    quantity integer DEFAULT 1 NOT NULL,
    loan_duration_days integer DEFAULT 0 NOT NULL,
    delivery_address text NOT NULL,
    next_run_date timestamp with time zone NOT NULL,
    last_run_date timestamp with time zone,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    notes text,
    created_by_user_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_recurring_schedules material_recurring_schedules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_pkey PRIMARY KEY (id);


--
-- Name: idx_mat_recurring_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_recurring_org ON public.material_recurring_schedules USING btree (organization_id, status);


--
-- Name: material_recurring_schedules material_recurring_schedules_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_created_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_created_by_user_id_fkey FOREIGN KEY (created_by_user_id) REFERENCES public.users(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_offering_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_offering_id_fkey FOREIGN KEY (offering_id) REFERENCES public.material_offerings(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: material_recurring_schedules material_recurring_schedules_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict eHblnhaKdze43sQrtVXnuRGKWoAPc8wKoVOHLYLXgf2dKtNJP2Qw64xNkP5CFLZ

