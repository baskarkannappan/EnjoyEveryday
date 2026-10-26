--
-- PostgreSQL database dump
--

\restrict ca6wEh8chuFoSkPHUmSNj6sqlO7gGyhNBIqCxh0AZpkKDqbWWkZsUcSeybuJVE6

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
-- Name: user_external_logins; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_external_logins (
    provider character varying(100) NOT NULL,
    provider_key character varying(256) NOT NULL,
    user_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: user_external_logins user_external_logins_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_external_logins
    ADD CONSTRAINT user_external_logins_pkey PRIMARY KEY (provider, provider_key);


--
-- Name: idx_user_external_logins_user; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_user_external_logins_user ON public.user_external_logins USING btree (user_id);


--
-- Name: user_external_logins user_external_logins_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_external_logins
    ADD CONSTRAINT user_external_logins_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict ca6wEh8chuFoSkPHUmSNj6sqlO7gGyhNBIqCxh0AZpkKDqbWWkZsUcSeybuJVE6

