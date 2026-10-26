--
-- PostgreSQL database dump
--

\restrict iZgBXLaZZ8XtnQHs3EnWWTCmys0IT6KcTbOwaqLhEbzNkNaWPORsSfhZPWXGwFa

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
-- Name: child_milestones; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_milestones (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    classroom_id uuid,
    teacher_id uuid NOT NULL,
    title character varying(255) NOT NULL,
    description text,
    milestone_date timestamp with time zone NOT NULL,
    development_area_id uuid,
    evidence_summary text,
    journey_entry_id uuid,
    parent_visible boolean DEFAULT false NOT NULL,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by uuid,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_by uuid
);


--
-- Name: child_milestones child_milestones_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_pkey PRIMARY KEY (id);


--
-- Name: idx_child_milestones_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_milestones_child ON public.child_milestones USING btree (child_id);


--
-- Name: idx_child_milestones_portfolio; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_milestones_portfolio ON public.child_milestones USING btree (tenant_id, child_id, parent_visible, milestone_date DESC);


--
-- Name: idx_child_milestones_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_milestones_tenant ON public.child_milestones USING btree (tenant_id);


--
-- Name: child_milestones child_milestones_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_milestones child_milestones_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE SET NULL;


--
-- Name: child_milestones child_milestones_development_area_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_development_area_id_fkey FOREIGN KEY (development_area_id) REFERENCES public.development_areas(id) ON DELETE SET NULL;


--
-- Name: child_milestones child_milestones_journey_entry_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_journey_entry_id_fkey FOREIGN KEY (journey_entry_id) REFERENCES public.child_journey_entries(id) ON DELETE SET NULL;


--
-- Name: child_milestones child_milestones_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_teacher_id_fkey FOREIGN KEY (teacher_id) REFERENCES public.teachers(id);


--
-- Name: child_milestones child_milestones_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict iZgBXLaZZ8XtnQHs3EnWWTCmys0IT6KcTbOwaqLhEbzNkNaWPORsSfhZPWXGwFa

