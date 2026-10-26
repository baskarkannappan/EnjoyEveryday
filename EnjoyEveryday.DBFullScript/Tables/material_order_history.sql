--
-- PostgreSQL database dump
--

\restrict CBX8XTcEYsk7JdlIq4dRB8kbTgWRzi5bp7Se7G1XArjYlTzcrAsEc70hZbyxweb

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
-- Name: material_order_history; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_order_history (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    event_type character varying(100) NOT NULL,
    previous_status character varying(50),
    new_status character varying(50),
    actor_user_id uuid,
    actor_email character varying(256),
    actor_role character varying(50) DEFAULT 'DaycareOrganization'::character varying NOT NULL,
    description text NOT NULL,
    notes text,
    is_internal_note boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_order_history material_order_history_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_history
    ADD CONSTRAINT material_order_history_pkey PRIMARY KEY (id);


--
-- Name: idx_mat_order_hist_order; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_order_hist_order ON public.material_order_history USING btree (order_id, created_at);


--
-- Name: material_order_history material_order_history_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_history
    ADD CONSTRAINT material_order_history_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict CBX8XTcEYsk7JdlIq4dRB8kbTgWRzi5bp7Se7G1XArjYlTzcrAsEc70hZbyxweb

