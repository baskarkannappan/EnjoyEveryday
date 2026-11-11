--
-- PostgreSQL database dump
--

\restrict 898q6nYG6GHKnrfakkUp0Bqcetxsb3WnpIz8jDTTq5V2h81KVCO8LoxgIlBKYsu

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
-- Name: tenant_preferences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tenant_preferences (
    tenant_id uuid NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    time_zone character varying(100) DEFAULT 'Asia/Kolkata'::character varying NOT NULL,
    operating_hours_json jsonb DEFAULT '{"openTime": "08:00", "closeTime": "18:00", "workingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]}'::jsonb NOT NULL,
    statutory_ratios_json jsonb DEFAULT '{"Infant": 4, "Toddler": 7, "Preschool": 10, "Kindergarten": 15}'::jsonb NOT NULL,
    billing_rules_json jsonb DEFAULT '{"invoiceDueDays": 15, "gracePeriodMinutes": 15, "latePickupFeePerMinute": 1.0, "siblingDiscountPercent": 10.0}'::jsonb NOT NULL,
    safety_policies_json jsonb DEFAULT '{"minEmergencyContacts": 2, "napCheckIntervalMinutes": 15, "requireDualMedicationSignoff": false}'::jsonb NOT NULL,
    custom_settings_json jsonb DEFAULT '{}'::jsonb NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: tenant_preferences tenant_preferences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tenant_preferences
    ADD CONSTRAINT tenant_preferences_pkey PRIMARY KEY (tenant_id);


--
-- Name: tenant_preferences tenant_preferences_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tenant_preferences
    ADD CONSTRAINT tenant_preferences_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 898q6nYG6GHKnrfakkUp0Bqcetxsb3WnpIz8jDTTq5V2h81KVCO8LoxgIlBKYsu

