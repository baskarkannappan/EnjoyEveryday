--
-- PostgreSQL database dump
--

\restrict 7CSLVUHGCmkUY6ymKYdWvywPRYjSX7U0XYfDYggJFFBusLpbzK6B7xlvHEucFtG

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
-- Name: child_food_restrictions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_food_restrictions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    food character varying(200) NOT NULL,
    restriction_type character varying(100) NOT NULL,
    severity character varying(50),
    notes text
);


--
-- Name: child_food_restrictions child_food_restrictions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_food_restrictions
    ADD CONSTRAINT child_food_restrictions_pkey PRIMARY KEY (id);


--
-- Name: child_food_restrictions child_food_restrictions_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_food_restrictions
    ADD CONSTRAINT child_food_restrictions_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_food_restrictions child_food_restrictions_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_food_restrictions
    ADD CONSTRAINT child_food_restrictions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 7CSLVUHGCmkUY6ymKYdWvywPRYjSX7U0XYfDYggJFFBusLpbzK6B7xlvHEucFtG

