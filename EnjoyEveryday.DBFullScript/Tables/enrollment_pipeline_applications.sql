--
-- PostgreSQL database dump
--

\restrict 1u5NJMT1FcNm0nR1QcWt6kcAG0lcCtlwwYtloV4JjCRHRRNdbMjZCFOuBcKOhwj

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
-- Name: enrollment_pipeline_applications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.enrollment_pipeline_applications (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid,
    child_first_name character varying(100) NOT NULL,
    child_last_name character varying(100) NOT NULL,
    date_of_birth date NOT NULL,
    gender character varying(20),
    parent_guardian_name character varying(150) NOT NULL,
    parent_email character varying(150) NOT NULL,
    parent_phone character varying(50) NOT NULL,
    target_classroom_id uuid,
    desired_start_date date,
    schedule_type character varying(50) DEFAULT 'FullTime'::character varying NOT NULL,
    pipeline_stage character varying(50) DEFAULT 'Enquiry'::character varying NOT NULL,
    tour_date timestamp with time zone,
    tuition_monthly_rate numeric(10,2) DEFAULT 850.00 NOT NULL,
    deposit_amount numeric(10,2) DEFAULT 200.00 NOT NULL,
    sibling_discount_percent numeric(5,2) DEFAULT 0,
    registration_fee numeric(10,2) DEFAULT 50.00,
    internal_notes text,
    child_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_pkey PRIMARY KEY (id);


--
-- Name: idx_enrollment_pipeline_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_enrollment_pipeline_branch ON public.enrollment_pipeline_applications USING btree (branch_id);


--
-- Name: idx_enrollment_pipeline_tenant_stage; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_enrollment_pipeline_tenant_stage ON public.enrollment_pipeline_applications USING btree (tenant_id, pipeline_stage);


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE SET NULL;


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_target_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_target_classroom_id_fkey FOREIGN KEY (target_classroom_id) REFERENCES public.classrooms(id);


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 1u5NJMT1FcNm0nR1QcWt6kcAG0lcCtlwwYtloV4JjCRHRRNdbMjZCFOuBcKOhwj

