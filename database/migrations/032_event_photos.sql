-- ============================================================
-- EnjoyEveryday — Migration 032: Event Photos Support
-- Adds: photo_urls TEXT[] to store up to 3 AVIF photo URLs/Base64 strings
-- ============================================================

ALTER TABLE events 
ADD COLUMN IF NOT EXISTS photo_urls TEXT[] DEFAULT '{}';
