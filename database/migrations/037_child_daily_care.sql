-- 037_child_daily_care.sql
-- Child Daily Care & Wellbeing Module

-- 1. CHILD CARE EVENTS (Diapers, Meals, Toileting, Hygiene, Comfort, Sleep notes)
CREATE TABLE IF NOT EXISTS child_care_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    branch_id UUID NOT NULL REFERENCES branches(id),
    classroom_id UUID REFERENCES classrooms(id),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    event_type VARCHAR(50) NOT NULL, -- DiaperChange, Meal, SleepRest, Toileting, Hygiene, Comfort
    event_time TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Diaper details
    diaper_condition VARCHAR(50), -- Wet, Soiled, WetAndSoiled, DryCheck
    skin_observation VARCHAR(200),
    
    -- Meal & feeding details
    meal_type VARCHAR(50), -- Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner, Bottle, Water
    food_offered VARCHAR(250),
    amount_consumed VARCHAR(50), -- All, Most, Some, None, NotRecorded
    fluid_oz NUMERIC(4, 1),
    
    -- Sleep / Rest details
    rest_status VARCHAR(50), -- FellAsleep, Rested, DifficultySettling, WokeNaturally, Awakened
    
    -- Toileting details
    toileting_activity VARCHAR(50), -- Attempt, Success, Accident, Assistance
    
    -- Hygiene details
    hygiene_type VARCHAR(50), -- Handwashing, FaceCleaning, ClothingChange, AccidentCleaning, PersonalCare
    
    -- Comfort & wellbeing
    comfort_mood VARCHAR(50), -- Happy, Settled, NeededComfort, Tired, QuietRest, UpsetSupported
    
    -- Common fields & metadata
    notes TEXT,
    parent_visible BOOLEAN NOT NULL DEFAULT TRUE,
    recorded_by_user_id UUID NOT NULL REFERENCES users(id),
    recorded_by_name VARCHAR(150),
    correction_reason TEXT,
    is_corrected BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_cce_child_time ON child_care_events(child_id, event_time DESC);
CREATE INDEX IF NOT EXISTS idx_cce_classroom_date ON child_care_events(classroom_id, event_time);
CREATE INDEX IF NOT EXISTS idx_cce_tenant_org ON child_care_events(tenant_id, organization_id);

-- 2. CHILD NAP SESSIONS (Sleep & Rest start/stop workflow)
CREATE TABLE IF NOT EXISTS child_nap_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    branch_id UUID NOT NULL REFERENCES branches(id),
    classroom_id UUID REFERENCES classrooms(id),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    duration_minutes INT,
    status VARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, Completed, Cancelled
    outcome VARCHAR(50), -- WokeNaturally, Awakened, RestedOnly, Restless
    notes TEXT,
    started_by_user_id UUID NOT NULL REFERENCES users(id),
    started_by_name VARCHAR(150),
    ended_by_user_id UUID REFERENCES users(id),
    ended_by_name VARCHAR(150),
    parent_visible BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_cns_child_active ON child_nap_sessions(child_id, status);

-- 3. CHILD CARE PLANS (Individual routines, allergies link, feeding guidance)
CREATE TABLE IF NOT EXISTS child_care_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    branch_id UUID NOT NULL REFERENCES branches(id),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    version INT NOT NULL DEFAULT 1,
    diaper_routine_guidance TEXT,
    feeding_instructions TEXT,
    bottle_schedule_guidance TEXT,
    sleep_routine_guidance TEXT,
    toileting_support_plan TEXT,
    comfort_calming_preferences TEXT,
    required_supplies_note TEXT,
    dietary_allergies_summary TEXT, -- snapshot summary linked to child_allergies / dietary tables
    effective_date DATE NOT NULL DEFAULT CURRENT_DATE,
    review_date DATE,
    status VARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, Superseded, PendingApproval
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    approved_by_user_id UUID REFERENCES users(id),
    approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_ccp_child ON child_care_plans(child_id, status);

-- 4. CHILD CARE SUPPLIES (Personal care supply replenishment tracking)
CREATE TABLE IF NOT EXISTS child_care_supplies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    branch_id UUID NOT NULL REFERENCES branches(id),
    classroom_id UUID REFERENCES classrooms(id),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    supply_type VARCHAR(50) NOT NULL, -- Diapers, Wipes, SpareClothes, Bottles, Ointment, ComfortItem, Other
    status VARCHAR(50) NOT NULL DEFAULT 'Adequate', -- Adequate, Low, Depleted
    notes TEXT,
    threshold_note VARCHAR(150),
    last_reported_by_user_id UUID NOT NULL REFERENCES users(id),
    last_reported_by_name VARCHAR(150),
    parent_reminder_sent BOOLEAN NOT NULL DEFAULT FALSE,
    last_reminder_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_ccs_child ON child_care_supplies(child_id, status);

-- 5. SEED INITIAL SAMPLE CARE EVENTS FOR DEMO CHILD
DO $$
DECLARE
    v_tenant_id UUID;
    v_org_id UUID;
    v_branch_id UUID;
    v_classroom_id UUID;
    v_child_id UUID;
    v_user_id UUID;
BEGIN
    SELECT id INTO v_tenant_id FROM tenants LIMIT 1;
    SELECT id INTO v_org_id FROM organizations LIMIT 1;
    SELECT id INTO v_branch_id FROM branches LIMIT 1;
    SELECT id INTO v_classroom_id FROM classrooms LIMIT 1;
    SELECT id INTO v_child_id FROM children LIMIT 1;
    SELECT id INTO v_user_id FROM users LIMIT 1;

    IF v_child_id IS NOT NULL AND v_user_id IS NOT NULL THEN
        -- Seed care plan if not exists
        IF NOT EXISTS (SELECT 1 FROM child_care_plans WHERE child_id = v_child_id) THEN
            INSERT INTO child_care_plans (
                id, tenant_id, organization_id, branch_id, child_id, version,
                diaper_routine_guidance, feeding_instructions, sleep_routine_guidance,
                comfort_calming_preferences, required_supplies_note, dietary_allergies_summary,
                effective_date, review_date, status, created_by_user_id
            ) VALUES (
                gen_random_uuid(), v_tenant_id, v_org_id, v_branch_id, v_child_id, 1,
                'Check diaper every 2 hours; apply barrier cream if redness appears.',
                'Self-feeds finger foods; cut grapes into quarters. Offer water after meals.',
                'Likes quiet lullaby; rests comfortably with soft blanket from 12:30 PM to 2:00 PM.',
                'Enjoys holding small plush star when settling down.',
                'Size 4 diapers, unscented water wipes, two sets of spare clothes in cubby.',
                'No known food allergies. Vegetarian preferences recorded.',
                CURRENT_DATE - INTERVAL '14 days', CURRENT_DATE + INTERVAL '160 days', 'Active', v_user_id
            );
        END IF;

        -- Seed supplies if not exists
        IF NOT EXISTS (SELECT 1 FROM child_care_supplies WHERE child_id = v_child_id) THEN
            INSERT INTO child_care_supplies (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                supply_type, status, threshold_note, last_reported_by_user_id, last_reported_by_name
            ) VALUES 
            (gen_random_uuid(), v_tenant_id, v_org_id, v_branch_id, v_classroom_id, v_child_id, 'Diapers', 'Adequate', '18 diapers in cubby', v_user_id, 'Teacher Sarah'),
            (gen_random_uuid(), v_tenant_id, v_org_id, v_branch_id, v_classroom_id, v_child_id, 'Wipes', 'Low', '1 half-pack remaining', v_user_id, 'Teacher Sarah'),
            (gen_random_uuid(), v_tenant_id, v_org_id, v_branch_id, v_classroom_id, v_child_id, 'SpareClothes', 'Adequate', '2 spare outfits available', v_user_id, 'Teacher Sarah');
        END IF;

        -- Seed sample care events for today
        IF NOT EXISTS (SELECT 1 FROM child_care_events WHERE child_id = v_child_id AND event_time::date = CURRENT_DATE) THEN
            -- Diaper change at 9:15 AM
            INSERT INTO child_care_events (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                event_type, event_time, diaper_condition, notes, parent_visible,
                recorded_by_user_id, recorded_by_name
            ) VALUES (
                gen_random_uuid(), v_tenant_id, v_org_id, v_branch_id, v_classroom_id, v_child_id,
                'DiaperChange', CURRENT_DATE + TIME '09:15:00', 'Wet', 'Wet diaper; changed by assigned caregiver.', TRUE,
                v_user_id, 'Teacher Sarah'
            );

            -- Lunch at 11:45 AM
            INSERT INTO child_care_events (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                event_type, event_time, meal_type, food_offered, amount_consumed, notes, parent_visible,
                recorded_by_user_id, recorded_by_name
            ) VALUES (
                gen_random_uuid(), v_tenant_id, v_org_id, v_branch_id, v_classroom_id, v_child_id,
                'Meal', CURRENT_DATE + TIME '11:45:00', 'Lunch', 'Steamed carrots, rice & lentil bowl, pear slices', 'Most', 'Ate most of the meal; drank water.', TRUE,
                v_user_id, 'Teacher Sarah'
            );

            -- Nap start at 12:30 PM
            INSERT INTO child_care_events (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                event_type, event_time, rest_status, notes, parent_visible,
                recorded_by_user_id, recorded_by_name
            ) VALUES (
                gen_random_uuid(), v_tenant_id, v_org_id, v_branch_id, v_classroom_id, v_child_id,
                'SleepRest', CURRENT_DATE + TIME '12:30:00', 'FellAsleep', 'Rest period started. Settled easily with soft blanket.', TRUE,
                v_user_id, 'Teacher Sarah'
            );

            -- Comfort / mood observation
            INSERT INTO child_care_events (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                event_type, event_time, comfort_mood, notes, parent_visible,
                recorded_by_user_id, recorded_by_name
            ) VALUES (
                gen_random_uuid(), v_tenant_id, v_org_id, v_branch_id, v_classroom_id, v_child_id,
                'Comfort', CURRENT_DATE + TIME '10:30:00', 'Happy', 'Enjoyed building blocks and playing with friends.', TRUE,
                v_user_id, 'Teacher Sarah'
            );

            -- Nap session completed (80 min)
            INSERT INTO child_nap_sessions (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                start_time, end_time, duration_minutes, status, outcome, notes,
                started_by_user_id, started_by_name, ended_by_user_id, ended_by_name, parent_visible
            ) VALUES (
                gen_random_uuid(), v_tenant_id, v_org_id, v_branch_id, v_classroom_id, v_child_id,
                CURRENT_DATE + TIME '12:30:00', CURRENT_DATE + TIME '13:50:00', 80, 'Completed', 'WokeNaturally',
                'Peaceful, uninterrupted rest.', v_user_id, 'Teacher Sarah', v_user_id, 'Teacher Sarah', TRUE
            );
        END IF;
    END IF;
END $$;
