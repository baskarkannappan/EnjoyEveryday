--
-- PostgreSQL database dump
--

\restrict JzzHtz74fX6t6tF93VxWHv6XfxOaGBGKyDLr655GozYxSl7DBRYpxLQG2q2Gc7L

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
-- Name: marketplace_orders; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.marketplace_orders (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    platform_experience_id uuid NOT NULL,
    purchased_by_user_id uuid NOT NULL,
    pricing_model character varying(50) NOT NULL,
    amount_paid numeric(10,2) DEFAULT 0.00 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    payment_status character varying(50) DEFAULT 'Completed'::character varying NOT NULL,
    transaction_reference character varying(100) NOT NULL,
    purchased_at timestamp with time zone DEFAULT now() NOT NULL,
    refunded_at timestamp with time zone,
    refund_reason text
);


--
-- Name: marketplace_orders marketplace_orders_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_pkey PRIMARY KEY (id);


--
-- Name: marketplace_orders uq_marketplace_orders_txn; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT uq_marketplace_orders_txn UNIQUE (organization_id, platform_experience_id, transaction_reference);


--
-- Name: idx_marketplace_orders_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_marketplace_orders_exp ON public.marketplace_orders USING btree (platform_experience_id);


--
-- Name: idx_marketplace_orders_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_marketplace_orders_org ON public.marketplace_orders USING btree (organization_id);


--
-- Name: marketplace_orders marketplace_orders_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: marketplace_orders marketplace_orders_platform_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_platform_experience_id_fkey FOREIGN KEY (platform_experience_id) REFERENCES public.platform_experiences(id) ON DELETE CASCADE;


--
-- Name: marketplace_orders marketplace_orders_purchased_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_purchased_by_user_id_fkey FOREIGN KEY (purchased_by_user_id) REFERENCES public.users(id);


--
-- Name: marketplace_orders marketplace_orders_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict JzzHtz74fX6t6tF93VxWHv6XfxOaGBGKyDLr655GozYxSl7DBRYpxLQG2q2Gc7L

