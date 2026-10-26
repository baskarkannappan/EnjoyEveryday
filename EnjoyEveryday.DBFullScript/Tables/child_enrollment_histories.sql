--
-- PostgreSQL database dump
--

\restrict F8CZN90X86taf0xD1KfaaG6koFyAdQDoFMCaeceTkbLdKcxzKkRBGkCm4ItYxx8

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
-- Name: child_enrollment_histories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_enrollment_histories (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    classroom_id uuid,
    start_date date NOT NULL,
    end_date date,
    reason text
);


--
-- Name: child_enrollment_histories child_enrollment_histories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_enrollment_histories
    ADD CONSTRAINT child_enrollment_histories_pkey PRIMARY KEY (id);


--
-- Name: child_enrollment_histories child_enrollment_histories_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_enrollment_histories
    ADD CONSTRAINT child_enrollment_histories_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_enrollment_histories child_enrollment_histories_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_enrollment_histories
    ADD CONSTRAINT child_enrollment_histories_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_enrollment_histories child_enrollment_histories_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_enrollment_histories
    ADD CONSTRAINT child_enrollment_histories_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict F8CZN90X86taf0xD1KfaaG6koFyAdQDoFMCaeceTkbLdKcxzKkRBGkCm4ItYxx8

