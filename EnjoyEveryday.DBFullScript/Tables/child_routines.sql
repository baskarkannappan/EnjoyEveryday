--
-- PostgreSQL database dump
--

\restrict NYhf2qJjRGJ4qPwJLjB9a4G5Y7KmUtylrXA19g7CruLkGYiegew6IstxJns5aoe

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
-- Name: child_routines; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_routines (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    morning_routine text,
    comfort_strategies text,
    favorite_activities text,
    favorite_toys text,
    favorite_books text,
    calming_strategies text,
    communication_preferences text,
    transition_strategies text,
    other_notes text
);


--
-- Name: child_routines child_routines_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_routines
    ADD CONSTRAINT child_routines_pkey PRIMARY KEY (child_id);


--
-- Name: child_routines child_routines_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_routines
    ADD CONSTRAINT child_routines_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_routines child_routines_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_routines
    ADD CONSTRAINT child_routines_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- PostgreSQL database dump complete
--

\unrestrict NYhf2qJjRGJ4qPwJLjB9a4G5Y7KmUtylrXA19g7CruLkGYiegew6IstxJns5aoe

