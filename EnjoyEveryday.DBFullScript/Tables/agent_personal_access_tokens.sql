--
-- PostgreSQL database dump
--

\restrict N9X8ASypH3okb5SduGqhmLi8qRZOdcyPV84fUOTw3TwMJ86xvTUcdRv7OPJaHbK

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
-- Name: agent_personal_access_tokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.agent_personal_access_tokens (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    token_name character varying(100) NOT NULL,
    token_prefix character varying(32) NOT NULL,
    token_hash character varying(128) NOT NULL,
    role character varying(50) NOT NULL,
    scopes text[] DEFAULT '{}'::text[] NOT NULL,
    is_revoked boolean DEFAULT false NOT NULL,
    expires_at timestamp with time zone,
    last_used_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: agent_personal_access_tokens agent_personal_access_tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.agent_personal_access_tokens
    ADD CONSTRAINT agent_personal_access_tokens_pkey PRIMARY KEY (id);


--
-- Name: agent_personal_access_tokens agent_personal_access_tokens_token_hash_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.agent_personal_access_tokens
    ADD CONSTRAINT agent_personal_access_tokens_token_hash_key UNIQUE (token_hash);


--
-- Name: idx_agent_tokens_hash; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_agent_tokens_hash ON public.agent_personal_access_tokens USING btree (token_hash) WHERE (is_revoked = false);


--
-- Name: idx_agent_tokens_tenant_user; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_agent_tokens_tenant_user ON public.agent_personal_access_tokens USING btree (tenant_id, user_id);


--
-- Name: agent_personal_access_tokens agent_personal_access_tokens_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.agent_personal_access_tokens
    ADD CONSTRAINT agent_personal_access_tokens_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: agent_personal_access_tokens agent_personal_access_tokens_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.agent_personal_access_tokens
    ADD CONSTRAINT agent_personal_access_tokens_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict N9X8ASypH3okb5SduGqhmLi8qRZOdcyPV84fUOTw3TwMJ86xvTUcdRv7OPJaHbK

