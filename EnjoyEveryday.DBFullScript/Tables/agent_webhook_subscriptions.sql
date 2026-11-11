--
-- PostgreSQL database dump
--

\restrict gMRP1uBIjRgBzxr1greUbOgAvKEk7VhAnHVFXdxtKwMiwVg5mUb26WOucEu5nxH

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
-- Name: agent_webhook_subscriptions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.agent_webhook_subscriptions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    name character varying(100) NOT NULL,
    webhook_url text NOT NULL,
    secret_key character varying(128) NOT NULL,
    subscribed_events text[] DEFAULT '{}'::text[] NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    last_triggered_at timestamp with time zone,
    failure_count integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: agent_webhook_subscriptions agent_webhook_subscriptions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.agent_webhook_subscriptions
    ADD CONSTRAINT agent_webhook_subscriptions_pkey PRIMARY KEY (id);


--
-- Name: idx_agent_webhooks_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_agent_webhooks_tenant ON public.agent_webhook_subscriptions USING btree (tenant_id) WHERE (is_active = true);


--
-- Name: agent_webhook_subscriptions agent_webhook_subscriptions_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.agent_webhook_subscriptions
    ADD CONSTRAINT agent_webhook_subscriptions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: agent_webhook_subscriptions agent_webhook_subscriptions_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.agent_webhook_subscriptions
    ADD CONSTRAINT agent_webhook_subscriptions_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict gMRP1uBIjRgBzxr1greUbOgAvKEk7VhAnHVFXdxtKwMiwVg5mUb26WOucEu5nxH

