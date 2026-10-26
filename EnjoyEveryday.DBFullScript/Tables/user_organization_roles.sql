--
-- PostgreSQL database dump
--

\restrict EWRlINSigR94oGVlBNEgmJevSg0NLQKLWlnuJzPc7iQ5jADHTTOMPaP4kW5nY5i

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
-- Name: user_organization_roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_organization_roles (
    user_id uuid NOT NULL,
    role_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    tenant_id uuid NOT NULL
);


--
-- Name: user_organization_roles user_organization_roles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_organization_roles
    ADD CONSTRAINT user_organization_roles_pkey PRIMARY KEY (user_id, role_id, organization_id);


--
-- Name: user_organization_roles user_organization_roles_audit; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER user_organization_roles_audit AFTER INSERT OR DELETE ON public.user_organization_roles FOR EACH ROW EXECUTE FUNCTION public.audit_org_role_change();


--
-- Name: user_organization_roles user_organization_roles_role_id_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_organization_roles
    ADD CONSTRAINT user_organization_roles_role_id_tenant_id_fkey FOREIGN KEY (role_id, tenant_id) REFERENCES public.roles(id, tenant_id) ON DELETE CASCADE;


--
-- Name: user_organization_roles user_organization_roles_user_id_organization_id_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_organization_roles
    ADD CONSTRAINT user_organization_roles_user_id_organization_id_tenant_id_fkey FOREIGN KEY (user_id, organization_id, tenant_id) REFERENCES public.organization_memberships(user_id, organization_id, tenant_id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict EWRlINSigR94oGVlBNEgmJevSg0NLQKLWlnuJzPc7iQ5jADHTTOMPaP4kW5nY5i

