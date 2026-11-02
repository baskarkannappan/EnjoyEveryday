-- Migration: 100_experiences_intelligence_settings_users_lookups.sql
-- Seed master lookups for Experiences, Intelligence, Organization Roster, Safety, Settings & Agent Integration, and Users

INSERT INTO master_lookups (category, code, display_name, display_order, is_active, metadata_json)
VALUES
    -- Celebration & Event Types
    ('CelebrationType', 'Birthday', 'Birthday Celebration 🎂', 1, true, '{"badge_color":"#f59e0b"}'),
    ('CelebrationType', 'Festival', 'Cultural Festival 🏮', 2, true, '{"badge_color":"#8b5cf6"}'),
    ('CelebrationType', 'Graduation', 'Preschool Graduation 🎓', 3, true, '{"badge_color":"#10b981"}'),
    ('CelebrationType', 'Seasonal', 'Seasonal Celebration 🍂', 4, true, '{"badge_color":"#f97316"}'),
    ('CelebrationType', 'FamilyDay', 'Family & Community Day 👨‍👩‍👧', 5, true, '{"badge_color":"#06b6d4"}'),
    ('CelebrationType', 'Milestone', 'Learning Milestone Celebration 🌟', 6, true, '{"badge_color":"#557c6c"}'),

    -- Pedagogical Approaches
    ('PedagogicalApproach', 'ReggioEmilia', 'Reggio Emilia Inspired', 1, true, '{"description":"Child-led, environment as third teacher, documentation-focused"}'),
    ('PedagogicalApproach', 'Montessori', 'Montessori Method', 2, true, '{"description":"Self-directed activity, hands-on learning, collaborative play"}'),
    ('PedagogicalApproach', 'ForestSchool', 'Forest & Nature School', 3, true, '{"description":"Outdoor discovery, risk-benefit learning, seasonal nature immersion"}'),
    ('PedagogicalApproach', 'Waldorf', 'Waldorf Steiner', 4, true, '{"description":"Artistic, practical, and intellectual skills in holistic balance"}'),
    ('PedagogicalApproach', 'Emergent', 'Emergent Inquiry', 5, true, '{"description":"Responsive curriculum evolving from child curiosities and interests"}'),

    -- Staff Leave Types (Organization Roster)
    ('StaffLeaveType', 'Vacation', 'Annual Vacation / Paid Time Off', 1, true, '{"icon":"🌴","requires_coverage":true}'),
    ('StaffLeaveType', 'Sick', 'Medical & Sick Leave', 2, true, '{"icon":"🤒","requires_coverage":true}'),
    ('StaffLeaveType', 'Personal', 'Personal / Family Care Day', 3, true, '{"icon":"🏡","requires_coverage":true}'),
    ('StaffLeaveType', 'Training', 'Professional Pedagogy Training', 4, true, '{"icon":"📚","requires_coverage":true}'),
    ('StaffLeaveType', 'MaternityPaternity', 'Parental / Family Bond Leave', 5, true, '{"icon":"👶","requires_coverage":true}'),
    ('StaffLeaveType', 'Bereavement', 'Compassionate & Bereavement Leave', 6, true, '{"icon":"🕊️","requires_coverage":true}'),

    -- AI Agent Token Scopes (Settings / Agent Management)
    ('AgentTokenScope', 'mcp:full_access', 'Full MCP Protocol Access', 1, true, '{"description":"Unrestricted read/write access via Model Context Protocol"}'),
    ('AgentTokenScope', 'read:children', 'Read Enrolled Children', 2, true, '{"description":"Access student names, classrooms, and daily wellbeing summaries"}'),
    ('AgentTokenScope', 'write:dailycare', 'Record Daily Care Events', 3, true, '{"description":"Submit diaper, meal, rest, and comfort observations"}'),
    ('AgentTokenScope', 'read:curriculum', 'Read Curriculum & Experiences', 4, true, '{"description":"Inspect active lesson plans, material lists, and activities"}'),
    ('AgentTokenScope', 'write:curriculum', 'Author Curriculum Plans', 5, true, '{"description":"Draft and adapt experience sequences and learning milestones"}'),
    ('AgentTokenScope', 'read:messages', 'Read Parent Inquiries', 6, true, '{"description":"Review inbound parent messages and channel threads"}'),
    ('AgentTokenScope', 'write:messages', 'Draft Parent Responses', 7, true, '{"description":"Draft AI replies for educator review and approval"}'),

    -- Outbound Domain Event Webhook Types (Settings / Webhooks)
    ('AgentWebhookEventType', 'child.enrolled', 'Child Enrollment Finalized', 1, true, '{"event_source":"Admissions"}'),
    ('AgentWebhookEventType', 'dailycare.recorded', 'Daily Care Event Logged', 2, true, '{"event_source":"DailyCare"}'),
    ('AgentWebhookEventType', 'attendance.marked', 'Attendance Status Changed', 3, true, '{"event_source":"Attendance"}'),
    ('AgentWebhookEventType', 'message.sent', 'Classroom Message Sent', 4, true, '{"event_source":"Communication"}'),
    ('AgentWebhookEventType', 'incident.reported', 'Safety Incident Logged', 5, true, '{"event_source":"SafetyTrust"}'),
    ('AgentWebhookEventType', 'consent.signed', 'Electronic Consent Executed', 6, true, '{"event_source":"SafetyTrust"}'),

    -- User Account Statuses
    ('UserAccountStatus', 'Active', 'Active & Verified', 1, true, '{"color":"#16a34a"}'),
    ('UserAccountStatus', 'PendingInvite', 'Pending Verification / Invitation Sent', 2, true, '{"color":"#f59e0b"}'),
    ('UserAccountStatus', 'Suspended', 'Suspended by Administrator', 3, true, '{"color":"#dc2626"}'),
    ('UserAccountStatus', 'Deactivated', 'Archived / Deactivated', 4, true, '{"color":"#6c7870"}')
ON CONFLICT (category, code) DO UPDATE
SET display_name = EXCLUDED.display_name,
    display_order = EXCLUDED.display_order,
    is_active = EXCLUDED.is_active,
    metadata_json = EXCLUDED.metadata_json;
