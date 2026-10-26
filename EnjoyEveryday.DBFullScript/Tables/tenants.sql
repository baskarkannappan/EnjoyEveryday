--
-- PostgreSQL database dump
--

\restrict 9KQ3AmGEdO9ZkBaWyn59C0IJBkTW02nsoPmUypMfnHXfoIlMlkqmOTv5sYI7bCP

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
-- Name: tenants; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tenants (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying(200) NOT NULL,
    slug character varying(100) NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    encrypted_ai_api_key character varying(1000)
);


--
-- Name: tenants tenants_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tenants
    ADD CONSTRAINT tenants_pkey PRIMARY KEY (id);


--
-- Name: tenants tenants_slug_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tenants
    ADD CONSTRAINT tenants_slug_key UNIQUE (slug);


--
-- Name: tenants tenants_ensure_roles; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tenants_ensure_roles AFTER INSERT ON public.tenants FOR EACH ROW EXECUTE FUNCTION public.trg_tenants_ensure_roles();


--
-- PostgreSQL database dump complete
--

\unrestrict 9KQ3AmGEdO9ZkBaWyn59C0IJBkTW02nsoPmUypMfnHXfoIlMlkqmOTv5sYI7bCP

