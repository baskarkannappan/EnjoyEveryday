--
-- PostgreSQL database dump
--

\restrict VwXBXc4xRymHuZ5c5cxE0TgbSaWOpeInuXtLXdmMqAyziUsjGINVLv77EP6cIqB

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
-- Name: support_access_grants; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.support_access_grants (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    granted_to_user_id uuid NOT NULL,
    granted_by_user_id uuid,
    purpose character varying(500) NOT NULL,
    read_only boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    revoked_at timestamp with time zone,
    CONSTRAINT chk_support_grant_window CHECK (((expires_at > created_at) AND (expires_at <= (created_at + '7 days'::interval)))),
    CONSTRAINT support_access_grants_purpose_check CHECK ((length(TRIM(BOTH FROM purpose)) > 0))
);


--
-- Name: support_access_grants support_access_grants_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.support_access_grants
    ADD CONSTRAINT support_access_grants_pkey PRIMARY KEY (id);


--
-- Name: idx_support_access_grants_lookup; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_support_access_grants_lookup ON public.support_access_grants USING btree (tenant_id, granted_to_user_id) WHERE (revoked_at IS NULL);


--
-- Name: support_access_grants support_access_grants_granted_to_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.support_access_grants
    ADD CONSTRAINT support_access_grants_granted_to_user_id_fkey FOREIGN KEY (granted_to_user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: support_access_grants support_access_grants_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.support_access_grants
    ADD CONSTRAINT support_access_grants_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict VwXBXc4xRymHuZ5c5cxE0TgbSaWOpeInuXtLXdmMqAyziUsjGINVLv77EP6cIqB

