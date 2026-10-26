--
-- PostgreSQL database dump
--

\restrict o5jvJ2xu0BCxc0A73REfeT0xULoVgs6bWn9dQLxBJQWzHe9RKLbsCm2zF0Ud2ua

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
-- Name: child_journey_developments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_developments (
    journey_entry_id uuid NOT NULL,
    development_area_id uuid NOT NULL
);


--
-- Name: child_journey_developments child_journey_developments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_developments
    ADD CONSTRAINT child_journey_developments_pkey PRIMARY KEY (journey_entry_id, development_area_id);


--
-- Name: child_journey_developments child_journey_developments_development_area_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_developments
    ADD CONSTRAINT child_journey_developments_development_area_id_fkey FOREIGN KEY (development_area_id) REFERENCES public.development_areas(id) ON DELETE CASCADE;


--
-- Name: child_journey_developments child_journey_developments_journey_entry_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_developments
    ADD CONSTRAINT child_journey_developments_journey_entry_id_fkey FOREIGN KEY (journey_entry_id) REFERENCES public.child_journey_entries(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict o5jvJ2xu0BCxc0A73REfeT0xULoVgs6bWn9dQLxBJQWzHe9RKLbsCm2zF0Ud2ua

