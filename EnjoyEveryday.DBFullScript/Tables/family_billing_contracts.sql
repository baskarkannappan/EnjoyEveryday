--
-- PostgreSQL database dump
--

\restrict ak4HFCWCbUcfjmwcuFlOQ8JSIacKdMe9Sh1Z5lEwObdAj95WKVziRmi7nFnRNDf

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
-- Name: family_billing_contracts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.family_billing_contracts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid,
    application_id uuid,
    child_id uuid NOT NULL,
    parent_name character varying(150) NOT NULL,
    parent_email character varying(150) NOT NULL,
    schedule_type character varying(50) DEFAULT 'FullTime'::character varying NOT NULL,
    billing_cycle character varying(50) DEFAULT 'Monthly'::character varying NOT NULL,
    base_rate numeric(10,2) NOT NULL,
    discount_percent numeric(5,2) DEFAULT 0,
    effective_rate numeric(10,2) NOT NULL,
    deposit_held numeric(10,2) DEFAULT 0,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    start_date date NOT NULL,
    end_date date,
    auto_invoice boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: family_billing_contracts family_billing_contracts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_pkey PRIMARY KEY (id);


--
-- Name: idx_family_contracts_tenant_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_family_contracts_tenant_child ON public.family_billing_contracts USING btree (tenant_id, child_id);


--
-- Name: family_billing_contracts family_billing_contracts_application_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_application_id_fkey FOREIGN KEY (application_id) REFERENCES public.enrollment_pipeline_applications(id) ON DELETE SET NULL;


--
-- Name: family_billing_contracts family_billing_contracts_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: family_billing_contracts family_billing_contracts_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: family_billing_contracts family_billing_contracts_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict ak4HFCWCbUcfjmwcuFlOQ8JSIacKdMe9Sh1Z5lEwObdAj95WKVziRmi7nFnRNDf

