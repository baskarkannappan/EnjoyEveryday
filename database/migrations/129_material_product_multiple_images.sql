-- Migration 129: Enable multiple images and widening image storage for material products

ALTER TABLE material_products ALTER COLUMN image_url TYPE text;
ALTER TABLE material_products ADD COLUMN IF NOT EXISTS image_urls text[] DEFAULT '{}'::text[];

-- Update sp_create_material_product and sp_update_material_product to preserve text type and arrays
CREATE OR REPLACE PROCEDURE sp_create_material_product(
    p_id uuid,
    p_sku varchar,
    p_name varchar,
    p_description text,
    p_category varchar,
    p_image_url text,
    p_material_type varchar,
    p_safety_instructions text,
    p_age_min_months integer,
    p_age_max_months integer,
    p_usage_restrictions text,
    p_cleaning_instructions text,
    p_return_policy text,
    p_is_active boolean,
    p_created_at timestamp with time zone,
    p_updated_at timestamp with time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_products (
        id, sku, name, description, category, image_url, material_type,
        safety_instructions, age_min_months, age_max_months, usage_restrictions,
        cleaning_instructions, return_policy, is_active, created_at, updated_at
    ) VALUES (
        p_id, p_sku, p_name, p_description, p_category, p_image_url, p_material_type,
        p_safety_instructions, p_age_min_months, p_age_max_months, p_usage_restrictions,
        p_cleaning_instructions, p_return_policy, p_is_active, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_material_product(
    p_id uuid,
    p_name varchar,
    p_description text,
    p_category varchar,
    p_image_url text,
    p_material_type varchar,
    p_safety_instructions text,
    p_age_min_months integer,
    p_age_max_months integer,
    p_usage_restrictions text,
    p_cleaning_instructions text,
    p_return_policy text,
    p_is_active boolean,
    p_updated_at timestamp with time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE material_products SET
        name = p_name,
        description = p_description,
        category = p_category,
        image_url = p_image_url,
        material_type = p_material_type,
        safety_instructions = p_safety_instructions,
        age_min_months = p_age_min_months,
        age_max_months = p_age_max_months,
        usage_restrictions = p_usage_restrictions,
        cleaning_instructions = p_cleaning_instructions,
        return_policy = p_return_policy,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id;
END;
$$;

