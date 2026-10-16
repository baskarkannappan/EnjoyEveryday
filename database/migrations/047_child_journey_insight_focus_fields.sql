-- Migration 047: Child Journey Insights Focus and Question Fields
ALTER TABLE child_journey_insight_requests 
ADD COLUMN IF NOT EXISTS focus_area VARCHAR(100),
ADD COLUMN IF NOT EXISTS teacher_question TEXT;
