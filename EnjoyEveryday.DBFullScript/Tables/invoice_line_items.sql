--
-- PostgreSQL database dump
--

\restrict a4txTTwcHSzPdZJIi4AdAD2aQNKlyDoS62i83wJDiqIkxy7RDjZStRWwqrDSwJn

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
-- Name: invoice_line_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.invoice_line_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    invoice_id uuid NOT NULL,
    line_number integer DEFAULT 1 NOT NULL,
    description character varying(300) NOT NULL,
    quantity numeric(10,2) DEFAULT 1 NOT NULL,
    unit_price numeric(12,2) DEFAULT 0 NOT NULL,
    line_total numeric(12,2) DEFAULT 0 NOT NULL,
    category character varying(50) DEFAULT 'Subscription'::character varying,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: invoice_line_items invoice_line_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invoice_line_items
    ADD CONSTRAINT invoice_line_items_pkey PRIMARY KEY (id);


--
-- Name: idx_invoice_line_items_inv; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_invoice_line_items_inv ON public.invoice_line_items USING btree (invoice_id);


--
-- Name: invoice_line_items invoice_line_items_invoice_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invoice_line_items
    ADD CONSTRAINT invoice_line_items_invoice_id_fkey FOREIGN KEY (invoice_id) REFERENCES public.invoices(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict a4txTTwcHSzPdZJIi4AdAD2aQNKlyDoS62i83wJDiqIkxy7RDjZStRWwqrDSwJn

