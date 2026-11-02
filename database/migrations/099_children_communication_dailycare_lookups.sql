-- Migration 099: Children Enrollment, Communication Templates, and Daily Care Lookups

INSERT INTO public.master_lookups (category, code, display_name, display_order, is_active, metadata_json)
VALUES
    -- Enrollment Stages
    ('EnrollmentStage', 'Inquiry', 'New Inquiry', 1, true, '{"badgeClass": "blue", "isPipelineActive": true}'::jsonb),
    ('EnrollmentStage', 'TourScheduled', 'Tour Scheduled', 2, true, '{"badgeClass": "amber", "isPipelineActive": true}'::jsonb),
    ('EnrollmentStage', 'ApplicationSubmitted', 'Application Submitted', 3, true, '{"badgeClass": "purple", "isPipelineActive": true}'::jsonb),
    ('EnrollmentStage', 'Waitlisted', 'Waitlisted', 4, true, '{"badgeClass": "gray", "isPipelineActive": true}'::jsonb),
    ('EnrollmentStage', 'OfferExtended', 'Offer Extended', 5, true, '{"badgeClass": "green", "isPipelineActive": true}'::jsonb),
    ('EnrollmentStage', 'Enrolled', 'Active Contract Enrolled', 6, true, '{"badgeClass": "green", "isPipelineActive": false}'::jsonb),
    ('EnrollmentStage', 'Archived', 'Archived / Withdrawn', 7, true, '{"badgeClass": "gray", "isPipelineActive": false}'::jsonb),

    -- Enrollment Schedule Types
    ('EnrollmentScheduleType', 'FullTime', 'Full Time (5 days)', 1, true, '{"daysPerWeek": 5}'::jsonb),
    ('EnrollmentScheduleType', 'PartTime', 'Part Time (2-3 days)', 2, true, '{"daysPerWeek": 3}'::jsonb),
    ('EnrollmentScheduleType', 'DropIn', 'Drop In Care', 3, true, '{"daysPerWeek": 1}'::jsonb),

    -- Guardian Relationships
    ('GuardianRelationship', 'Mother', 'Mother', 1, true, '{"isPrimaryGuardian": true}'::jsonb),
    ('GuardianRelationship', 'Father', 'Father', 2, true, '{"isPrimaryGuardian": true}'::jsonb),
    ('GuardianRelationship', 'Guardian', 'Legal Guardian', 3, true, '{"isPrimaryGuardian": true}'::jsonb),
    ('GuardianRelationship', 'Grandparent', 'Grandparent', 4, true, '{"isPrimaryGuardian": false}'::jsonb),
    ('GuardianRelationship', 'Other', 'Other Authorized Relative', 5, true, '{"isPrimaryGuardian": false}'::jsonb),

    -- Toilet Training Statuses
    ('ToiletTrainingStatus', 'NotStarted', 'Not Started', 1, true, '{"tag": "routine"}'::jsonb),
    ('ToiletTrainingStatus', 'InProgress', 'In Progress', 2, true, '{"tag": "routine"}'::jsonb),
    ('ToiletTrainingStatus', 'FullyTrained', 'Fully Trained', 3, true, '{"tag": "routine"}'::jsonb),

    -- Communication Quick Response Templates
    ('QuickResponseTemplate', 'NapUpdate', 'Nap Routine Update', 1, true, '{"text": "Good afternoon! Just a quick update that your child had a restful nap today and woke up cheerful. 🌿", "shortcut": "nap"}'::jsonb),
    ('QuickResponseTemplate', 'MealUpdate', 'Meal & Appetite', 2, true, '{"text": "Your child enjoyed a healthy meal today and finished all portions with great enthusiasm. 🍎", "shortcut": "meal"}'::jsonb),
    ('QuickResponseTemplate', 'SuppliesReminder', 'Cubby Supplies Needed', 3, true, '{"text": "Friendly reminder: your child is running low on extra wipes/diapers. Could you please send extra in their cubby tomorrow? Thank you! 🎒", "shortcut": "supplies"}'::jsonb),
    ('QuickResponseTemplate', 'ReassuringDropoff', 'Morning Drop-off Reassurance', 4, true, '{"text": "Your child settled in quickly after drop-off and is happily engaged with friends and morning sensory exploration! 🎨", "shortcut": "dropoff"}'::jsonb),
    ('QuickResponseTemplate', 'ActivityMoment', 'Learning & Play Highlight', 5, true, '{"text": "We captured a lovely learning moment exploring our classroom activities today. Look for the story in their portfolio! 📸", "shortcut": "moment"}'::jsonb),

    -- Daily Care Activity Types
    ('CareActivityType', 'Diaper', 'Diaper Change', 1, true, '{"icon": "🧷", "requiresMeasurement": false}'::jsonb),
    ('CareActivityType', 'Toileting', 'Toileting Routine', 2, true, '{"icon": "🚽", "requiresMeasurement": false}'::jsonb),
    ('CareActivityType', 'Meal', 'Meal / Snack', 3, true, '{"icon": "🍎", "requiresMeasurement": true}'::jsonb),
    ('CareActivityType', 'Bottle', 'Bottle Feeding', 4, true, '{"icon": "🍼", "requiresMeasurement": true}'::jsonb),
    ('CareActivityType', 'Rest', 'Nap / Rest', 5, true, '{"icon": "💤", "requiresMeasurement": true}'::jsonb),
    ('CareActivityType', 'Hygiene', 'Hygiene / Handwash', 6, true, '{"icon": "🧼", "requiresMeasurement": false}'::jsonb),
    ('CareActivityType', 'Comfort', 'Comfort & Mood Observation', 7, true, '{"icon": "🌿", "requiresMeasurement": false}'::jsonb),

    -- Daily Care Supply Statuses
    ('CareSupplyStatus', 'Adequate', 'Adequate Supply', 1, true, '{"badgeClass": "green"}'::jsonb),
    ('CareSupplyStatus', 'Low', 'Running Low', 2, true, '{"badgeClass": "amber"}'::jsonb),
    ('CareSupplyStatus', 'Depleted', 'Depleted / Urgent', 3, true, '{"badgeClass": "red"}'::jsonb)
ON CONFLICT (category, code) DO UPDATE
SET display_name = EXCLUDED.display_name,
    display_order = EXCLUDED.display_order,
    metadata_json = EXCLUDED.metadata_json,
    updated_at = now();
