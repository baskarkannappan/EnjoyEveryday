-- ============================================================
-- EnjoyEveryday — Migration 052: Materials Marketplace and Warehouse Management
-- Creates: material_products, material_offerings, inventory_locations,
--          inventory_balances, inventory_ledger, material_orders,
--          material_order_items, material_order_history, material_shipments,
--          material_loans, material_return_inspections, material_recurring_schedules
-- Seeds: permissions and initial catalog + warehouse inventory
-- ============================================================

-- 1. PERMISSIONS SEED
INSERT INTO permissions (name, description) VALUES
    ('materials.view', 'Browse materials catalog, check availability, and view material details'),
    ('materials.order', 'Create, submit, accept quotations, pay, and track material purchases and loans'),
    ('platform.warehouse.manage', 'Manage central warehouse catalog, stock balances, movements, and inspections'),
    ('platform.marketplace.operations', 'Review requests, issue quotations, manage dispatch, and handle exceptions')
ON CONFLICT (name) DO NOTHING;

-- Grant permissions to PlatformAdmin system role
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name = 'PlatformAdmin'
  AND p.name IN ('materials.view', 'materials.order', 'platform.warehouse.manage', 'platform.marketplace.operations')
ON CONFLICT DO NOTHING;

-- Grant permissions to OrganizationOwner & OrganizationAdmin system roles
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher')
  AND p.name = 'materials.view'
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name IN ('OrganizationOwner', 'OrganizationAdmin')
  AND p.name = 'materials.order'
ON CONFLICT DO NOTHING;

-- Tenant-scoped roles
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NOT NULL 
  AND r.name IN ('OrganizationOwner', 'OrganizationAdmin')
  AND p.name IN ('materials.view', 'materials.order')
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NOT NULL 
  AND r.name IN ('BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher')
  AND p.name = 'materials.view'
ON CONFLICT DO NOTHING;

-- 2. MATERIAL PRODUCTS TABLE (Catalog)
CREATE TABLE IF NOT EXISTS material_products (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sku                     VARCHAR(64) NOT NULL UNIQUE,
    name                    VARCHAR(200) NOT NULL,
    description             TEXT NOT NULL,
    category                VARCHAR(100) NOT NULL DEFAULT 'General',
    image_url               VARCHAR(500),
    material_type           VARCHAR(50) NOT NULL DEFAULT 'Consumable', -- Consumable, Reusable, ActivityKit
    safety_instructions     TEXT,
    age_min_months          INT,
    age_max_months          INT,
    usage_restrictions      TEXT,
    cleaning_instructions   TEXT,
    return_policy           TEXT,
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mat_prod_cat ON material_products(category);
CREATE INDEX IF NOT EXISTS idx_mat_prod_type ON material_products(material_type);
CREATE INDEX IF NOT EXISTS idx_mat_prod_active ON material_products(is_active);

-- 3. MATERIAL OFFERINGS TABLE (Commercial terms & buy/borrow pricing)
CREATE TABLE IF NOT EXISTS material_offerings (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    material_product_id         UUID NOT NULL REFERENCES material_products(id) ON DELETE CASCADE,
    allow_purchase              BOOLEAN NOT NULL DEFAULT TRUE,
    purchase_price              NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    allow_borrow                BOOLEAN NOT NULL DEFAULT FALSE,
    rental_fee                  NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    refundable_deposit          NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    default_loan_duration_days  INT NOT NULL DEFAULT 14,
    max_loan_duration_days      INT NOT NULL DEFAULT 30,
    currency                    VARCHAR(10) NOT NULL DEFAULT 'INR',
    is_active                   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uk_mat_offering_prod UNIQUE (material_product_id)
);

-- 4. INVENTORY LOCATIONS TABLE
CREATE TABLE IF NOT EXISTS inventory_locations (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code                    VARCHAR(50) NOT NULL UNIQUE,
    name                    VARCHAR(150) NOT NULL,
    address                 TEXT,
    contact_phone           VARCHAR(50),
    is_central_warehouse    BOOLEAN NOT NULL DEFAULT TRUE,
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 5. INVENTORY BALANCES TABLE (Multi-state stock)
CREATE TABLE IF NOT EXISTS inventory_balances (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    material_product_id         UUID NOT NULL REFERENCES material_products(id) ON DELETE CASCADE,
    location_id                 UUID NOT NULL REFERENCES inventory_locations(id) ON DELETE CASCADE,
    total_stock                 INT NOT NULL DEFAULT 0,
    available_stock             INT NOT NULL DEFAULT 0,
    reserved_stock              INT NOT NULL DEFAULT 0,
    dispatched_stock            INT NOT NULL DEFAULT 0,
    on_loan_stock               INT NOT NULL DEFAULT 0,
    under_inspection_stock      INT NOT NULL DEFAULT 0,
    under_cleaning_stock        INT NOT NULL DEFAULT 0,
    damaged_stock               INT NOT NULL DEFAULT 0,
    missing_stock               INT NOT NULL DEFAULT 0,
    retired_stock               INT NOT NULL DEFAULT 0,
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uk_inv_bal_prod_loc UNIQUE (material_product_id, location_id),
    CONSTRAINT chk_avail_nonneg CHECK (available_stock >= 0),
    CONSTRAINT chk_reserv_nonneg CHECK (reserved_stock >= 0),
    CONSTRAINT chk_loan_nonneg CHECK (on_loan_stock >= 0)
);

CREATE INDEX IF NOT EXISTS idx_inv_bal_prod ON inventory_balances(material_product_id);

-- 6. INVENTORY LEDGER TABLE (Append-only movement transactions)
CREATE TABLE IF NOT EXISTS inventory_ledger (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    material_product_id     UUID NOT NULL REFERENCES material_products(id) ON DELETE CASCADE,
    location_id             UUID NOT NULL REFERENCES inventory_locations(id) ON DELETE CASCADE,
    transaction_type        VARCHAR(50) NOT NULL, -- Receipt, Reservation, ReservationRelease, DispatchPurchase, DispatchLoan, ReturnReceived, InspectionApproved, InspectionDamaged, InspectionMissing, CleaningComplete, Adjustment, Retirement
    quantity_delta          INT NOT NULL DEFAULT 0,
    available_delta         INT NOT NULL DEFAULT 0,
    reserved_delta          INT NOT NULL DEFAULT 0,
    on_loan_delta           INT NOT NULL DEFAULT 0,
    reference_type          VARCHAR(50), -- Order, Loan, Return, Adjustment
    reference_id            UUID,
    notes                   TEXT,
    performed_by_user_id    UUID,
    performed_by_email      VARCHAR(256),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_inv_ledger_prod ON inventory_ledger(material_product_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_inv_ledger_ref ON inventory_ledger(reference_type, reference_id);

-- 7. MATERIAL ORDERS TABLE (Shared authoritative order workflow)
CREATE TABLE IF NOT EXISTS material_orders (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number            VARCHAR(50) NOT NULL UNIQUE,
    tenant_id               UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id         UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    branch_id               UUID REFERENCES branches(id) ON DELETE SET NULL,
    classroom_id            UUID REFERENCES classrooms(id) ON DELETE SET NULL,
    planned_experience_id   UUID REFERENCES experiences(id) ON DELETE SET NULL,
    requesting_user_id      UUID NOT NULL REFERENCES users(id),
    requesting_user_email   VARCHAR(256),
    order_type              VARCHAR(50) NOT NULL DEFAULT 'StandardCatalog', -- StandardCatalog, ApprovalRequired, SpecialRequest
    workflow_status         VARCHAR(50) NOT NULL DEFAULT 'RequestSubmitted', 
    -- Allowed: RequestSubmitted, UnderReview, QuotationOffered, AwaitingConfirmation, Confirmed,
    --          PaymentPending, PaymentConfirmed, StockAllocated, PickingPacking, Dispatched,
    --          InTransit, Delivered, ReceiptConfirmed, Completed, Rejected, Cancelled, DeliveryException
    payment_status          VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Completed, Partial, Waived, Refunded
    fulfilment_status       VARCHAR(50) NOT NULL DEFAULT 'Unfulfilled', -- Unfulfilled, Allocated, Dispatched, Delivered, ReceiptConfirmed
    total_purchase_amount   NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    total_rental_fee        NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    total_deposit           NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    shipping_fee            NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    tax_amount              NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    total_amount            NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    currency                VARCHAR(10) NOT NULL DEFAULT 'INR',
    quotation_notes         TEXT,
    quotation_valid_until   TIMESTAMPTZ,
    rejection_reason        TEXT,
    cancellation_reason     TEXT,
    delivery_address        TEXT NOT NULL,
    delivery_contact_name   VARCHAR(150),
    delivery_contact_phone  VARCHAR(50),
    required_delivery_date  TIMESTAMPTZ,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mat_orders_org ON material_orders(organization_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_mat_orders_status ON material_orders(workflow_status);
CREATE INDEX IF NOT EXISTS idx_mat_orders_exp ON material_orders(planned_experience_id);

-- 8. MATERIAL ORDER ITEMS TABLE
CREATE TABLE IF NOT EXISTS material_order_items (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                UUID NOT NULL REFERENCES material_orders(id) ON DELETE CASCADE,
    material_product_id     UUID NOT NULL REFERENCES material_products(id),
    offering_id             UUID REFERENCES material_offerings(id),
    fulfilment_mode         VARCHAR(50) NOT NULL DEFAULT 'Purchase', -- Purchase, Borrow
    quantity                INT NOT NULL DEFAULT 1,
    unit_price              NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    rental_fee              NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    deposit                 NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    line_total              NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    loan_duration_days      INT NOT NULL DEFAULT 0,
    return_due_date         TIMESTAMPTZ,
    status                  VARCHAR(50) NOT NULL DEFAULT 'Pending',
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mat_order_items_order ON material_order_items(order_id);

-- 9. MATERIAL ORDER HISTORY TABLE (Shared Event Timeline)
CREATE TABLE IF NOT EXISTS material_order_history (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                UUID NOT NULL REFERENCES material_orders(id) ON DELETE CASCADE,
    event_type              VARCHAR(100) NOT NULL,
    previous_status         VARCHAR(50),
    new_status              VARCHAR(50),
    actor_user_id           UUID,
    actor_email             VARCHAR(256),
    actor_role              VARCHAR(50) NOT NULL DEFAULT 'DaycareOrganization', -- DaycareOrganization, PlatformAdmin, System
    description             TEXT NOT NULL,
    notes                   TEXT,
    is_internal_note        BOOLEAN NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mat_order_hist_order ON material_order_history(order_id, created_at ASC);

-- 10. MATERIAL SHIPMENTS TABLE
CREATE TABLE IF NOT EXISTS material_shipments (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                UUID NOT NULL REFERENCES material_orders(id) ON DELETE CASCADE,
    shipment_number         VARCHAR(50) NOT NULL UNIQUE,
    carrier_name            VARCHAR(100) NOT NULL,
    tracking_reference      VARCHAR(100) NOT NULL,
    dispatch_date           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    estimated_delivery_date TIMESTAMPTZ,
    actual_delivery_date    TIMESTAMPTZ,
    delivery_status         VARCHAR(50) NOT NULL DEFAULT 'Dispatched', -- Dispatched, InTransit, Delivered, Failed, Exception
    proof_of_delivery_notes TEXT,
    recorded_by_user_id     UUID REFERENCES users(id),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mat_shipments_order ON material_shipments(order_id);

-- 11. MATERIAL LOANS TABLE (Reusable Kit Tracking)
CREATE TABLE IF NOT EXISTS material_loans (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                UUID NOT NULL REFERENCES material_orders(id) ON DELETE CASCADE,
    order_item_id           UUID NOT NULL REFERENCES material_order_items(id) ON DELETE CASCADE,
    material_product_id     UUID NOT NULL REFERENCES material_products(id),
    organization_id         UUID NOT NULL REFERENCES organizations(id),
    classroom_id            UUID REFERENCES classrooms(id),
    quantity_issued         INT NOT NULL DEFAULT 1,
    issued_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    due_date                TIMESTAMPTZ NOT NULL,
    actual_return_date      TIMESTAMPTZ,
    return_status           VARCHAR(50) NOT NULL DEFAULT 'Reserved',
    -- Allowed: Reserved, Dispatched, Delivered, InUse, ReturnDue, ReturnInTransit,
    --          Received, Inspected, AvailableAgain, Damaged, Missing, Retired
    deposit_amount          NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    deposit_status          VARCHAR(50) NOT NULL DEFAULT 'Held', -- Held, Refunded, Forfeited, Waived
    notes                   TEXT,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mat_loans_org ON material_loans(organization_id, due_date);
CREATE INDEX IF NOT EXISTS idx_mat_loans_status ON material_loans(return_status);

-- 12. MATERIAL RETURN INSPECTIONS TABLE
CREATE TABLE IF NOT EXISTS material_return_inspections (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    loan_id                     UUID NOT NULL REFERENCES material_loans(id) ON DELETE CASCADE,
    order_id                    UUID NOT NULL REFERENCES material_orders(id),
    material_product_id         UUID NOT NULL REFERENCES material_products(id),
    returned_quantity           INT NOT NULL,
    inspected_at                TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    inspected_by_user_id        UUID REFERENCES users(id),
    inspected_by_email          VARCHAR(256),
    condition                   VARCHAR(50) NOT NULL DEFAULT 'Good', -- Excellent, Good, RequiresCleaning, Damaged, MissingParts, Unusable
    quantity_approved_for_stock INT NOT NULL DEFAULT 0,
    quantity_requiring_cleaning INT NOT NULL DEFAULT 0,
    quantity_damaged            INT NOT NULL DEFAULT 0,
    quantity_missing            INT NOT NULL DEFAULT 0,
    inspection_notes            TEXT,
    deposit_disposition         VARCHAR(50) NOT NULL DEFAULT 'FullRefund', -- FullRefund, PartialDeduction, FullForfeiture, NoDeposit
    deposit_deduction_amount    NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    resolution_status           VARCHAR(50) NOT NULL DEFAULT 'Completed', -- Completed, PendingAdminReview
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mat_inspections_loan ON material_return_inspections(loan_id);

-- 13. MATERIAL RECURRING SCHEDULES TABLE
CREATE TABLE IF NOT EXISTS material_recurring_schedules (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id         UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    branch_id               UUID REFERENCES branches(id),
    classroom_id            UUID REFERENCES classrooms(id),
    schedule_type           VARCHAR(50) NOT NULL DEFAULT 'RecurringPurchase', -- RecurringPurchase, RecurringLoan
    frequency               VARCHAR(50) NOT NULL DEFAULT 'Monthly', -- Weekly, BiWeekly, Monthly
    material_product_id     UUID NOT NULL REFERENCES material_products(id),
    offering_id             UUID REFERENCES material_offerings(id),
    quantity                INT NOT NULL DEFAULT 1,
    loan_duration_days      INT NOT NULL DEFAULT 0,
    delivery_address        TEXT NOT NULL,
    next_run_date           TIMESTAMPTZ NOT NULL,
    last_run_date           TIMESTAMPTZ,
    status                  VARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, Paused, Cancelled
    notes                   TEXT,
    created_by_user_id      UUID REFERENCES users(id),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mat_recurring_org ON material_recurring_schedules(organization_id, status);

-- 14. SEED INITIAL WAREHOUSE & PRODUCTS
INSERT INTO inventory_locations (id, code, name, address, contact_phone, is_central_warehouse, is_active)
VALUES 
    ('c0000000-0000-0000-0000-000000000001'::uuid, 'WH-CENTRAL-01', 'EnjoyEveryday Central Hub & Sanitization Warehouse', 'Warehouse Facility 4, Eco-Tech Logistics Park, Bangalore, Karnataka', '+91 80 4920 1100', TRUE, TRUE)
ON CONFLICT (code) DO NOTHING;

-- Seed Sample Products & Offerings
INSERT INTO material_products (id, sku, name, description, category, image_url, material_type, safety_instructions, age_min_months, age_max_months, usage_restrictions, cleaning_instructions, return_policy, is_active)
VALUES
    ('d0000000-0000-0000-0000-000000000001'::uuid, 'KIT-WATER-01', 'Water Exploration & Transfer Kit', 'Comprehensive sensory water activity kit including silicone graduated pipettes, non-slip acrylic basins, water funnels, transparent silicone tubing, floating measuring bowls, and splash-proof aprons.', 'Sensory & Science', 'images/materials/water-exploration-kit.jpg', 'ActivityKit', 'Adult supervision required during water play. Non-toxic, BPA-free food-grade silicone.', 24, 72, 'Indoor or outdoor smooth floor only.', 'Sanitize with warm soapy water and food-safe disinfectant spray. Dry thoroughly before packing.', '14-day loan kit. Must be returned clean and with all 8 accessories in the original container.', TRUE),
    
    ('d0000000-0000-0000-0000-000000000002'::uuid, 'KIT-WOOD-SORT', 'Montessori Wooden Sorting & Stacking Trays', 'Natural beechwood sorting trays, wooden tongs, and smooth organic counting discs designed for tactile fine-motor precision and cognitive categorization.', 'Montessori & Math', 'images/materials/montessori-trays.jpg', 'Reusable', 'Choking hazard for children under 18 months. Certified non-toxic beeswax finish.', 24, 60, 'Keep away from standing water or open flame.', 'Wipe with damp cloth and dry immediately. Do not submerge beechwood.', 'Reusable loan resource. Return all pieces in custom fitted wooden box.', TRUE),

    ('d0000000-0000-0000-0000-000000000003'::uuid, 'CON-PAINT-FINGER', 'Organic Non-Toxic Finger Paints (Set of 6)', 'Washable, food-grade vegetable pigment finger paints in 250ml squeeze bottles (Forest Green, Sun Yellow, Berry Red, Sky Blue, Earth Brown, Orange).', 'Arts & Crafts', 'images/materials/finger-paints.jpg', 'Consumable', 'Dermatologically tested. 100% hypoallergenic and washable with cold water.', 12, 72, 'Store in cool dry place below 30°C.', 'Disposable paint bottles; no cleaning required upon consumption.', 'Consumable purchase. Daycare retains supplies.', TRUE),

    ('d0000000-0000-0000-0000-000000000004'::uuid, 'CON-CRAFT-PAPER', 'Eco Heavyweight Recycled Craft Paper (100 Sheets)', 'Heavyweight 200gsm textured craft paper in natural earthy tones. Perfect for watercolours, glueing, and collage activities.', 'Arts & Crafts', 'images/materials/craft-paper.jpg', 'Consumable', 'Safe for all age groups. Made from 100% recycled post-consumer fiber.', 12, 84, 'Keep dry.', 'N/A', 'Consumable purchase.', TRUE),

    ('d0000000-0000-0000-0000-000000000005'::uuid, 'KIT-LIGHT-TABLE', 'Ultra-Slim LED Sensory Light Pad & Geometric Translucents', 'A3 illuminated light pad with 3 light warmth levels, paired with 64 translucent geometric shapes and color-mixing paddles.', 'Sensory & STEM', 'images/materials/light-pad.jpg', 'ActivityKit', 'Low-voltage 5V USB-C power adapter included with safety break-away cord.', 36, 84, 'Classroom use under teacher supervision.', 'Clean surface with microfiber cloth and screen sanitizer.', 'Premium reusable rental kit. 21-day loan duration.', TRUE)
ON CONFLICT (sku) DO NOTHING;

-- Seed Offerings for the Products
INSERT INTO material_offerings (material_product_id, allow_purchase, purchase_price, allow_borrow, rental_fee, refundable_deposit, default_loan_duration_days, max_loan_duration_days, currency, is_active)
VALUES
    ('d0000000-0000-0000-0000-000000000001'::uuid, TRUE, 3200.00, TRUE, 450.00, 500.00, 14, 30, 'INR', TRUE),
    ('d0000000-0000-0000-0000-000000000002'::uuid, TRUE, 2400.00, TRUE, 350.00, 400.00, 14, 30, 'INR', TRUE),
    ('d0000000-0000-0000-0000-000000000003'::uuid, TRUE, 850.00, FALSE, 0.00, 0.00, 0, 0, 'INR', TRUE),
    ('d0000000-0000-0000-0000-000000000004'::uuid, TRUE, 650.00, FALSE, 0.00, 0.00, 0, 0, 'INR', TRUE),
    ('d0000000-0000-0000-0000-000000000005'::uuid, TRUE, 5500.00, TRUE, 750.00, 1000.00, 21, 45, 'INR', TRUE)
ON CONFLICT (material_product_id) DO NOTHING;

-- Seed Warehouse Balances
INSERT INTO inventory_balances (material_product_id, location_id, total_stock, available_stock, reserved_stock, dispatched_stock, on_loan_stock, under_inspection_stock, under_cleaning_stock, damaged_stock, missing_stock, retired_stock)
VALUES
    ('d0000000-0000-0000-0000-000000000001'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 25, 20, 2, 0, 3, 0, 0, 0, 0, 0),
    ('d0000000-0000-0000-0000-000000000002'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 30, 26, 1, 0, 3, 0, 0, 0, 0, 0),
    ('d0000000-0000-0000-0000-000000000003'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 150, 140, 10, 0, 0, 0, 0, 0, 0, 0),
    ('d0000000-0000-0000-0000-000000000004'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 200, 185, 15, 0, 0, 0, 0, 0, 0, 0),
    ('d0000000-0000-0000-0000-000000000005'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 15, 12, 1, 0, 2, 0, 0, 0, 0, 0)
ON CONFLICT (material_product_id, location_id) DO NOTHING;

-- Seed Initial Receipt Ledger Entries
INSERT INTO inventory_ledger (material_product_id, location_id, transaction_type, quantity_delta, available_delta, reserved_delta, on_loan_delta, reference_type, reference_id, notes, performed_by_email)
VALUES
    ('d0000000-0000-0000-0000-000000000001'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 'Receipt', 25, 25, 0, 0, 'InitialStock', NULL, 'Initial central warehouse receipt of Water Exploration Kits', 'admin@enjoyeveryday.com'),
    ('d0000000-0000-0000-0000-000000000002'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 'Receipt', 30, 30, 0, 0, 'InitialStock', NULL, 'Initial central warehouse receipt of Montessori Trays', 'admin@enjoyeveryday.com'),
    ('d0000000-0000-0000-0000-000000000003'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 'Receipt', 150, 150, 0, 0, 'InitialStock', NULL, 'Initial bulk stocking of finger paints', 'admin@enjoyeveryday.com'),
    ('d0000000-0000-0000-0000-000000000004'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 'Receipt', 200, 200, 0, 0, 'InitialStock', NULL, 'Initial bulk stocking of eco craft paper', 'admin@enjoyeveryday.com'),
    ('d0000000-0000-0000-0000-000000000005'::uuid, 'c0000000-0000-0000-0000-000000000001'::uuid, 'Receipt', 15, 15, 0, 0, 'InitialStock', NULL, 'Initial stocking of LED sensory light pads', 'admin@enjoyeveryday.com');
