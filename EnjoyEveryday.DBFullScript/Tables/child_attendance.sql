--
-- PostgreSQL database dump
--

\restrict jFuVy9DBpgYxNDbblPFVgmcN3h3kl8Kv948GqA1S9k5s34OVMSWBqUlVLvIJqYT

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
-- Name: child_attendance; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_attendance (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    child_id uuid NOT NULL,
    attendance_date date NOT NULL,
    status character varying(50) NOT NULL,
    arrival_time time without time zone,
    departure_time time without time zone,
    notes text,
    recorded_by_user_id uuid,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    pickup_person_name character varying(255),
    pickup_person_relationship character varying(100),
    pickup_verification_method character varying(100),
    pickup_notes text,
    authorized_pickup_id uuid,
    departure_verified_by_user_id uuid
);


--
-- Name: child_attendance child_attendance_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT child_attendance_pkey PRIMARY KEY (id);


--
-- Name: idx_ca_tenant_branch_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ca_tenant_branch_date ON public.child_attendance USING btree (tenant_id, branch_id, attendance_date);


--
-- Name: idx_ca_tenant_classroom_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ca_tenant_classroom_date ON public.child_attendance USING btree (tenant_id, classroom_id, attendance_date);


--
-- Name: idx_child_attendance_departure; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_attendance_departure ON public.child_attendance USING btree (tenant_id, child_id, attendance_date, departure_time);


--
-- Name: child_attendance child_attendance_authorized_pickup_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT child_attendance_authorized_pickup_id_fkey FOREIGN KEY (authorized_pickup_id) REFERENCES public.authorized_pickups(id) ON DELETE SET NULL;


--
-- Name: child_attendance child_attendance_departure_verified_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT child_attendance_departure_verified_by_user_id_fkey FOREIGN KEY (departure_verified_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: child_attendance fk_ca_branch; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT fk_ca_branch FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_attendance fk_ca_child; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT fk_ca_child FOREIGN KEY (child_id) REFERENCES public.children(id);


--
-- Name: child_attendance fk_ca_classroom; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT fk_ca_classroom FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_attendance fk_ca_tenant; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT fk_ca_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict jFuVy9DBpgYxNDbblPFVgmcN3h3kl8Kv948GqA1S9k5s34OVMSWBqUlVLvIJqYT

