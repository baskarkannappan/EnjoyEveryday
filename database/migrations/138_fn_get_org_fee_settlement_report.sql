-- Migration 138: Stored function for organization fee settlement and payment status reporting
CREATE OR REPLACE FUNCTION public.fn_get_org_fee_settlement_report(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL,
    p_child_id UUID DEFAULT NULL,
    p_billing_period VARCHAR DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL,
    p_search VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    child_id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    photo_url TEXT,
    classroom_id UUID,
    classroom_name VARCHAR,
    branch_id UUID,
    branch_name VARCHAR,
    currency VARCHAR,
    invoice_count INT,
    total_invoiced NUMERIC(14,2),
    total_paid NUMERIC(14,2),
    balance_due NUMERIC(14,2),
    pending_verification_amount NUMERIC(14,2),
    pending_verification_count INT,
    settlement_status VARCHAR,
    invoices_json TEXT,
    payments_json TEXT
)
LANGUAGE sql
STABLE
AS $function$
    WITH org_children AS (
        SELECT DISTINCT
            c.id AS c_child_id,
            c.first_name AS c_first_name,
            c.last_name AS c_last_name,
            c.photo_url AS c_photo_url,
            cl.id AS c_classroom_id,
            COALESCE(cl.name, 'Unassigned')::VARCHAR AS c_classroom_name,
            COALESCE(cl.branch_id, eb.id, i_br.branch_id) AS c_branch_id,
            COALESCE(b.name, eb.name, ib.name, 'Main Branch')::VARCHAR AS c_branch_name
        FROM children c
        LEFT JOIN classrooms cl ON c.classroom_id = cl.id
        LEFT JOIN branches b ON cl.branch_id = b.id
        LEFT JOIN enrollments e ON e.child_id = c.id
        LEFT JOIN branches eb ON e.branch_id = eb.id AND eb.organization_id = p_organization_id
        LEFT JOIN LATERAL (
            SELECT inv_br.branch_id FROM invoices inv_br
            WHERE inv_br.child_id = c.id AND inv_br.organization_id = p_organization_id AND inv_br.billing_domain = 'OrgToParent'
            LIMIT 1
        ) i_br ON TRUE
        LEFT JOIN branches ib ON i_br.branch_id = ib.id
        WHERE c.is_active = TRUE
          AND (
              b.organization_id = p_organization_id
              OR eb.organization_id = p_organization_id
              OR EXISTS (
                  SELECT 1 FROM invoices inv_chk
                  WHERE inv_chk.child_id = c.id
                    AND inv_chk.organization_id = p_organization_id
                    AND inv_chk.billing_domain = 'OrgToParent'
              )
          )
          AND (p_child_id IS NULL OR c.id = p_child_id)
          AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
          AND (p_branch_id IS NULL OR cl.branch_id = p_branch_id OR eb.id = p_branch_id OR i_br.branch_id = p_branch_id)
          AND (
              p_search IS NULL
              OR NULLIF(TRIM(p_search), '') IS NULL
              OR c.first_name ILIKE '%' || TRIM(p_search) || '%'
              OR c.last_name ILIKE '%' || TRIM(p_search) || '%'
              OR (c.first_name || ' ' || c.last_name) ILIKE '%' || TRIM(p_search) || '%'
          )
    ),
    child_invoices AS (
        SELECT
            inv.id,
            inv.child_id,
            inv.invoice_number,
            inv.billing_period,
            inv.subtotal,
            inv.discount_amount,
            inv.tax_amount,
            inv.total_amount,
            COALESCE(inv.adjustment_amount, 0) AS adjustment_amount,
            GREATEST(0, inv.total_amount - COALESCE(inv.adjustment_amount, 0)) AS effective_payable,
            COALESCE(inv.amount_paid, 0) AS amount_paid,
            GREATEST(0, (inv.total_amount - COALESCE(inv.adjustment_amount, 0)) - COALESCE(inv.amount_paid, 0)) AS balance_due,
            COALESCE(inv.pending_verification_amount, 0) AS pending_verification_amount,
            inv.currency,
            inv.status,
            inv.fee_method,
            inv.fee_plan_name,
            inv.issue_date,
            inv.due_date,
            inv.paid_at,
            inv.description,
            (
                SELECT COALESCE(json_agg(json_build_object(
                    'id', ii.id,
                    'installmentNumber', ii.installment_number,
                    'dueDate', ii.due_date,
                    'amountDue', ii.amount_due,
                    'notes', ii.notes
                ) ORDER BY ii.installment_number), '[]'::json)
                FROM invoice_installments ii
                WHERE ii.invoice_id = inv.id
            ) AS installments
        FROM invoices inv
        WHERE inv.organization_id = p_organization_id
          AND inv.billing_domain = 'OrgToParent'
          AND inv.status NOT IN ('Void', 'Cancelled', 'Draft')
          AND (p_billing_period IS NULL OR NULLIF(TRIM(p_billing_period), '') IS NULL OR inv.billing_period = p_billing_period)
          AND (p_branch_id IS NULL OR inv.branch_id = p_branch_id)
    ),
    child_payments AS (
        SELECT
            p.id,
            p.child_id,
            p.invoice_id,
            p.invoice_number,
            p.payment_reference,
            p.amount,
            p.currency,
            p.method,
            p.is_verified,
            p.verified_by,
            p.verified_at,
            p.status,
            p.transaction_id,
            p.notes,
            p.submission_source,
            p.proof_storage_key,
            p.payment_date,
            p.created_at
        FROM payments p
        WHERE p.organization_id = p_organization_id
          AND p.billing_domain = 'OrgToParent'
          AND p.is_reversed = FALSE
          AND (
              p.child_id IN (SELECT c_child_id FROM org_children)
              OR p.invoice_id IN (SELECT id FROM child_invoices)
          )
    ),
    aggregated AS (
        SELECT
            oc.c_child_id AS child_id,
            oc.c_first_name::VARCHAR AS first_name,
            oc.c_last_name::VARCHAR AS last_name,
            oc.c_photo_url AS photo_url,
            oc.c_classroom_id AS classroom_id,
            oc.c_classroom_name AS classroom_name,
            oc.c_branch_id AS branch_id,
            oc.c_branch_name AS branch_name,
            COALESCE(MAX(ci.currency), 'INR')::VARCHAR AS currency,
            COUNT(ci.id)::INT AS invoice_count,
            COALESCE(SUM(ci.effective_payable), 0)::NUMERIC(14,2) AS total_invoiced,
            COALESCE(SUM(ci.amount_paid), 0)::NUMERIC(14,2) AS total_paid,
            COALESCE(SUM(ci.balance_due), 0)::NUMERIC(14,2) AS balance_due,
            COALESCE(SUM(ci.pending_verification_amount), 0)::NUMERIC(14,2) AS pending_verification_amount,
            (
                SELECT COUNT(*)::INT FROM child_payments cp
                WHERE cp.child_id = oc.c_child_id AND cp.status = 'PendingVerification'
            ) AS pending_verification_count,
            CASE
                WHEN COUNT(ci.id) = 0 THEN 'NotInvoiced'
                WHEN (SELECT COUNT(*) FROM child_payments cp WHERE cp.child_id = oc.c_child_id AND cp.status = 'PendingVerification') > 0 THEN 'PendingVerification'
                WHEN COALESCE(SUM(ci.balance_due), 0) <= 0 THEN 'Paid'
                WHEN COALESCE(SUM(ci.amount_paid), 0) > 0 THEN 'PartiallyPaid'
                WHEN BOOL_OR(ci.due_date < NOW() AND ci.balance_due > 0) THEN 'Overdue'
                ELSE 'Unpaid'
            END::VARCHAR AS settlement_status,
            COALESCE((
                SELECT json_agg(ci_sub ORDER BY ci_sub.issue_date DESC)::TEXT
                FROM child_invoices ci_sub
                WHERE ci_sub.child_id = oc.c_child_id
            ), '[]') AS invoices_json,
            COALESCE((
                SELECT json_agg(cp_sub ORDER BY cp_sub.payment_date DESC)::TEXT
                FROM child_payments cp_sub
                WHERE cp_sub.child_id = oc.c_child_id
            ), '[]') AS payments_json
        FROM org_children oc
        LEFT JOIN child_invoices ci ON ci.child_id = oc.c_child_id
        GROUP BY oc.c_child_id, oc.c_first_name, oc.c_last_name, oc.c_photo_url, oc.c_classroom_id, oc.c_classroom_name, oc.c_branch_id, oc.c_branch_name
    )
    SELECT
        a.child_id,
        a.first_name,
        a.last_name,
        a.photo_url,
        a.classroom_id,
        a.classroom_name,
        a.branch_id,
        a.branch_name,
        a.currency,
        a.invoice_count,
        a.total_invoiced,
        a.total_paid,
        a.balance_due,
        a.pending_verification_amount,
        a.pending_verification_count,
        a.settlement_status,
        a.invoices_json,
        a.payments_json
    FROM aggregated a
    WHERE (
        p_status IS NULL OR NULLIF(TRIM(p_status), '') IS NULL OR p_status = 'All'
        OR (p_status = 'Paid' AND a.settlement_status = 'Paid')
        OR (p_status = 'PartiallyPaid' AND a.settlement_status = 'PartiallyPaid')
        OR (p_status = 'Unpaid' AND a.settlement_status IN ('Unpaid', 'Overdue'))
        OR (p_status = 'Overdue' AND a.settlement_status = 'Overdue')
        OR (p_status = 'PendingVerification' AND (a.settlement_status = 'PendingVerification' OR a.pending_verification_count > 0))
        OR (p_status = 'NotInvoiced' AND a.settlement_status = 'NotInvoiced')
    )
    ORDER BY
        CASE a.settlement_status
            WHEN 'PendingVerification' THEN 1
            WHEN 'Overdue' THEN 2
            WHEN 'Unpaid' THEN 3
            WHEN 'PartiallyPaid' THEN 4
            WHEN 'Paid' THEN 5
            ELSE 6
        END ASC,
        a.first_name ASC,
        a.last_name ASC;
$function$;

