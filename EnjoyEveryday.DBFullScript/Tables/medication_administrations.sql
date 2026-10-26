--
-- PostgreSQL database dump
--

\restrict 0o0FhpwRkUsW9SJPz8HXnHngGsLxybDs5Lnnpp3dStgTJscMQ7apd2fm9CFMF18

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
-- Name: medication_administrations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.medication_administrations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    medication_id uuid NOT NULL,
    child_id uuid NOT NULL,
    administered_at timestamp with time zone NOT NULL,
    dosage character varying(100),
    administered_by uuid NOT NULL,
    notes text
);


--
-- Name: medication_administrations medication_administrations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_pkey PRIMARY KEY (id);


--
-- Name: medication_administrations medication_administrations_administered_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_administered_by_fkey FOREIGN KEY (administered_by) REFERENCES public.users(id);


--
-- Name: medication_administrations medication_administrations_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: medication_administrations medication_administrations_medication_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_medication_id_fkey FOREIGN KEY (medication_id) REFERENCES public.child_medications(id) ON DELETE CASCADE;


--
-- Name: medication_administrations medication_administrations_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 0o0FhpwRkUsW9SJPz8HXnHngGsLxybDs5Lnnpp3dStgTJscMQ7apd2fm9CFMF18

