-- Migration 118: Vector Embeddings and Satellite Storage Prep
-- Prepares database for future AI agents without bloating core transactional tables
-- Supports PostgreSQL with or without pgvector extension installed

DO $$
BEGIN
    -- Safely enable pgvector if available on the host (e.g. Azure PostgreSQL Flexible Server)
    IF EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'vector') THEN
        CREATE EXTENSION IF NOT EXISTS vector;
    END IF;
END $$;

-- 1. Satellite Table: Child Journey Embeddings (Learning observations & developmental moments)
CREATE TABLE IF NOT EXISTS child_journey_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entry_id UUID NOT NULL REFERENCES child_journey_entries(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL,
    embedding_model VARCHAR(100) NOT NULL DEFAULT 'text-embedding-004',
    embedding_dimensions INT NOT NULL DEFAULT 768,
    embedding_vector TEXT NULL, -- Fallback storage for serialized vector when pgvector is not installed
    content_hash VARCHAR(64) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_child_journey_embeddings_tenant_entry 
ON child_journey_embeddings(tenant_id, entry_id);

-- 2. Satellite Table: Activity & Curriculum Embeddings (Lesson ideas, themes, and developmental fit)
CREATE TABLE IF NOT EXISTS activity_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    experience_id UUID NOT NULL,
    tenant_id UUID NOT NULL,
    embedding_model VARCHAR(100) NOT NULL DEFAULT 'text-embedding-004',
    embedding_dimensions INT NOT NULL DEFAULT 768,
    embedding_vector TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_activity_embeddings_tenant_exp 
ON activity_embeddings(tenant_id, experience_id);

-- 3. Satellite Table: Daycare Policy & FAQ Embeddings (Compliance, handbooks, and parent guidelines)
CREATE TABLE IF NOT EXISTS daycare_policy_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    document_type VARCHAR(100) NOT NULL,
    chunk_index INT NOT NULL DEFAULT 0,
    chunk_text TEXT NOT NULL,
    embedding_model VARCHAR(100) NOT NULL DEFAULT 'text-embedding-004',
    embedding_dimensions INT NOT NULL DEFAULT 768,
    embedding_vector TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_policy_embeddings_tenant_type 
ON daycare_policy_embeddings(tenant_id, document_type);

-- 4. Conditionally add native VECTOR(768) column and HNSW index if pgvector is active
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_type WHERE typname = 'vector') THEN
        -- Add vector columns if not present
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'child_journey_embeddings' AND column_name = 'embedding'
        ) THEN
            ALTER TABLE child_journey_embeddings ADD COLUMN embedding vector(768);
            EXECUTE 'CREATE INDEX IF NOT EXISTS idx_child_journey_embeddings_hnsw ON child_journey_embeddings USING hnsw (embedding vector_cosine_ops);';
        END IF;

        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'activity_embeddings' AND column_name = 'embedding'
        ) THEN
            ALTER TABLE activity_embeddings ADD COLUMN embedding vector(768);
            EXECUTE 'CREATE INDEX IF NOT EXISTS idx_activity_embeddings_hnsw ON activity_embeddings USING hnsw (embedding vector_cosine_ops);';
        END IF;

        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'daycare_policy_embeddings' AND column_name = 'embedding'
        ) THEN
            ALTER TABLE daycare_policy_embeddings ADD COLUMN embedding vector(768);
            EXECUTE 'CREATE INDEX IF NOT EXISTS idx_daycare_policy_embeddings_hnsw ON daycare_policy_embeddings USING hnsw (embedding vector_cosine_ops);';
        END IF;
    END IF;
END $$;

