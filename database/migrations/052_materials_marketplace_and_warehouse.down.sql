-- 052_materials_marketplace_and_warehouse.down.sql
-- Rollback for Materials Marketplace and Warehouse Management

DROP TABLE IF EXISTS material_recurring_schedules CASCADE;
DROP TABLE IF EXISTS material_return_inspections CASCADE;
DROP TABLE IF EXISTS material_loans CASCADE;
DROP TABLE IF EXISTS material_shipments CASCADE;
DROP TABLE IF EXISTS material_order_history CASCADE;
DROP TABLE IF EXISTS material_order_items CASCADE;
DROP TABLE IF EXISTS material_orders CASCADE;
DROP TABLE IF EXISTS inventory_ledger CASCADE;
DROP TABLE IF EXISTS inventory_balances CASCADE;
DROP TABLE IF EXISTS inventory_locations CASCADE;
DROP TABLE IF EXISTS material_offerings CASCADE;
DROP TABLE IF EXISTS material_products CASCADE;

DELETE FROM role_permissions WHERE permission_id IN (
    SELECT id FROM permissions WHERE name IN (
        'materials.view',
        'materials.order',
        'platform.warehouse.manage',
        'platform.marketplace.operations'
    )
);
DELETE FROM permissions WHERE name IN (
    'materials.view',
    'materials.order',
    'platform.warehouse.manage',
    'platform.marketplace.operations'
);
