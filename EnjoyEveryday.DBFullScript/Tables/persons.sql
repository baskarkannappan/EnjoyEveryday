--
-- PostgreSQL database dump
--

\restrict f7Sne577f7Cqh3pcJ1jRiA0xvgeQIjj1JVjKdI1Y0eZwBrg2wodCxu4yUSCna1x

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
-- Name: persons; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.persons (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    relationship character varying(100),
    email character varying(255),
    mobile_phone character varying(50),
    alternate_phone character varying(50),
    occupation character varying(100),
    employer character varying(100),
    preferred_contact_method character varying(50),
    preferred_language character varying(50),
    has_portal_access boolean DEFAULT false NOT NULL,
    receives_daily_updates boolean DEFAULT false NOT NULL,
    receives_photos boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: persons persons_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.persons
    ADD CONSTRAINT persons_pkey PRIMARY KEY (id);


--
-- Name: idx_persons_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_persons_tenant ON public.persons USING btree (tenant_id);


--
-- Name: persons persons_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.persons
    ADD CONSTRAINT persons_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: persons persons_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.persons
    ADD CONSTRAINT persons_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- PostgreSQL database dump complete
--

\unrestrict f7Sne577f7Cqh3pcJ1jRiA0xvgeQIjj1JVjKdI1Y0eZwBrg2wodCxu4yUSCna1x

