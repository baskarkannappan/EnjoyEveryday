--
-- PostgreSQL database dump
--

\restrict trT25zNMrg5zbDXpPRlBbe8MPtsCJtFk84snWRmtUOE1zvZGNZ1pfDPmgrIqqpF

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
-- Name: payments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.payments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    invoice_id uuid,
    invoice_number character varying(50),
    payment_reference character varying(100) NOT NULL,
    amount numeric(12,2) DEFAULT 0 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    method character varying(50) DEFAULT 'BankTransfer'::character varying NOT NULL,
    is_verified boolean DEFAULT true NOT NULL,
    verified_by character varying(200),
    verified_at timestamp with time zone,
    status character varying(50) DEFAULT 'Completed'::character varying NOT NULL,
    transaction_id character varying(100),
    notes text,
    payment_date timestamp with time zone DEFAULT now() NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    provider_order_id character varying(100),
    provider_payment_id character varying(100),
    provider_signature character varying(256),
    receipt_number character varying(100),
    recorded_by_user_id uuid,
    recorded_by_email character varying(256),
    is_reversed boolean DEFAULT false NOT NULL,
    reversed_at timestamp with time zone,
    reversed_by character varying(256),
    reversal_reason text,
    reversal_reference character varying(100),
    audit_metadata jsonb
);


--
-- Name: payments payments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_pkey PRIMARY KEY (id);


--
-- Name: idx_payments_invoice; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_payments_invoice ON public.payments USING btree (invoice_id);


--
-- Name: idx_payments_method_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_payments_method_date ON public.payments USING btree (method, payment_date DESC);


--
-- Name: idx_payments_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_payments_org ON public.payments USING btree (organization_id, tenant_id);


--
-- Name: idx_payments_provider_order_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_payments_provider_order_id ON public.payments USING btree (provider_order_id) WHERE (provider_order_id IS NOT NULL);


--
-- Name: uk_payments_provider_payment_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX uk_payments_provider_payment_id ON public.payments USING btree (provider_payment_id) WHERE ((provider_payment_id IS NOT NULL) AND (is_reversed = false));


--
-- Name: uk_payments_receipt_number; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX uk_payments_receipt_number ON public.payments USING btree (receipt_number) WHERE ((receipt_number IS NOT NULL) AND (is_reversed = false));


--
-- Name: payments payments_invoice_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_invoice_id_fkey FOREIGN KEY (invoice_id) REFERENCES public.invoices(id) ON DELETE SET NULL;


--
-- Name: payments payments_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: payments payments_recorded_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_recorded_by_user_id_fkey FOREIGN KEY (recorded_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: payments payments_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict trT25zNMrg5zbDXpPRlBbe8MPtsCJtFk84snWRmtUOE1zvZGNZ1pfDPmgrIqqpF

