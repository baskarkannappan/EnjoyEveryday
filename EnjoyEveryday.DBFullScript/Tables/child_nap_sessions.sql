--
-- PostgreSQL database dump
--

\restrict 9IkXsv5cMJ5Pjnlph8WL3v06HgrSgDboxwoUymxsvr0dcS25A143DsrQ3fY16bv

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
-- Name: child_nap_sessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_nap_sessions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid,
    child_id uuid NOT NULL,
    start_time timestamp with time zone NOT NULL,
    end_time timestamp with time zone,
    duration_minutes integer,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    outcome character varying(50),
    notes text,
    started_by_user_id uuid NOT NULL,
    started_by_name character varying(150),
    ended_by_user_id uuid,
    ended_by_name character varying(150),
    parent_visible boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_nap_sessions child_nap_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_pkey PRIMARY KEY (id);


--
-- Name: idx_cns_child_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cns_child_active ON public.child_nap_sessions USING btree (child_id, status);


--
-- Name: child_nap_sessions child_nap_sessions_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_nap_sessions child_nap_sessions_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_nap_sessions child_nap_sessions_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_nap_sessions child_nap_sessions_ended_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_ended_by_user_id_fkey FOREIGN KEY (ended_by_user_id) REFERENCES public.users(id);


--
-- Name: child_nap_sessions child_nap_sessions_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_nap_sessions child_nap_sessions_started_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_started_by_user_id_fkey FOREIGN KEY (started_by_user_id) REFERENCES public.users(id);


--
-- Name: child_nap_sessions child_nap_sessions_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict 9IkXsv5cMJ5Pjnlph8WL3v06HgrSgDboxwoUymxsvr0dcS25A143DsrQ3fY16bv

