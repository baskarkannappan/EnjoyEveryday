--
-- PostgreSQL database dump
--

\restrict NkDBbP5zaY2tdd57rv3qec2cwcujoQD9OvVnfaLZgiftcJZxaRHwJbfjNLlakMc

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
-- Name: teacher_handovers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_handovers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    handover_date date DEFAULT CURRENT_DATE NOT NULL,
    shift_type character varying(50) DEFAULT 'MorningToAfternoon'::character varying NOT NULL,
    outgoing_teacher_user_id uuid NOT NULL,
    outgoing_teacher_name character varying(200) NOT NULL,
    incoming_teacher_user_id uuid,
    incoming_teacher_name character varying(200),
    summary_notes text,
    child_specific_notes text,
    outstanding_care_tasks text,
    pending_followups text,
    status character varying(30) DEFAULT 'PendingAcknowledgement'::character varying NOT NULL,
    is_critical boolean DEFAULT false NOT NULL,
    acknowledged_at timestamp with time zone,
    acknowledged_by_user_id uuid,
    acknowledged_by_name character varying(200),
    acknowledgement_notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: teacher_handovers teacher_handovers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_pkey PRIMARY KEY (id);


--
-- Name: idx_teacher_handovers_classroom_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_teacher_handovers_classroom_date ON public.teacher_handovers USING btree (classroom_id, handover_date);


--
-- Name: idx_teacher_handovers_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_teacher_handovers_org ON public.teacher_handovers USING btree (tenant_id, organization_id, branch_id);


--
-- Name: idx_teacher_handovers_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_teacher_handovers_status ON public.teacher_handovers USING btree (status);


--
-- Name: teacher_handovers teacher_handovers_acknowledged_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_acknowledged_by_user_id_fkey FOREIGN KEY (acknowledged_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: teacher_handovers teacher_handovers_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id) ON DELETE CASCADE;


--
-- Name: teacher_handovers teacher_handovers_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: teacher_handovers teacher_handovers_incoming_teacher_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_incoming_teacher_user_id_fkey FOREIGN KEY (incoming_teacher_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: teacher_handovers teacher_handovers_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: teacher_handovers teacher_handovers_outgoing_teacher_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_outgoing_teacher_user_id_fkey FOREIGN KEY (outgoing_teacher_user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: teacher_handovers teacher_handovers_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict NkDBbP5zaY2tdd57rv3qec2cwcujoQD9OvVnfaLZgiftcJZxaRHwJbfjNLlakMc

