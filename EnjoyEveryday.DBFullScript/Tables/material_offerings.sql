--
-- PostgreSQL database dump
--

\restrict XY8jAegozQyWBqKXchrzibVR002ldXPv8f9sfDQWNMNfNrK3ilwrJZdMOF0CwLL

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
-- Name: material_offerings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_offerings (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    material_product_id uuid NOT NULL,
    allow_purchase boolean DEFAULT true NOT NULL,
    purchase_price numeric(10,2) DEFAULT 0.00 NOT NULL,
    allow_borrow boolean DEFAULT false NOT NULL,
    rental_fee numeric(10,2) DEFAULT 0.00 NOT NULL,
    refundable_deposit numeric(10,2) DEFAULT 0.00 NOT NULL,
    default_loan_duration_days integer DEFAULT 14 NOT NULL,
    max_loan_duration_days integer DEFAULT 30 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_offerings material_offerings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_offerings
    ADD CONSTRAINT material_offerings_pkey PRIMARY KEY (id);


--
-- Name: material_offerings uk_mat_offering_prod; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_offerings
    ADD CONSTRAINT uk_mat_offering_prod UNIQUE (material_product_id);


--
-- Name: material_offerings material_offerings_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_offerings
    ADD CONSTRAINT material_offerings_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict XY8jAegozQyWBqKXchrzibVR002ldXPv8f9sfDQWNMNfNrK3ilwrJZdMOF0CwLL

