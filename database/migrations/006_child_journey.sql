CREATE TABLE child_interests (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_child_interests_tenant ON child_interests(tenant_id);
CREATE INDEX idx_child_interests_child ON child_interests(child_id);

CREATE TABLE child_stories (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    teacher_id UUID NOT NULL,
    experience_schedule_id UUID REFERENCES experience_schedules(id) ON DELETE SET NULL,
    content TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_child_stories_tenant ON child_stories(tenant_id);
CREATE INDEX idx_child_stories_child ON child_stories(child_id);
