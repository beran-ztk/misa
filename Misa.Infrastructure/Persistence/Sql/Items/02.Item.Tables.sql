CREATE TABLE entities
(
    id                  UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_id            UUID,
    workflow_id         INT NOT NULL REFERENCES entity_workflow_types(id) ON DELETE RESTRICT,
    
    is_deleted          BOOL NOT NULL DEFAULT FALSE,
    is_archived         BOOL NOT NULL DEFAULT FALSE,
    
    created_at_utc      TIMESTAMPTZ NOT NULL,
    updated_at_utc      TIMESTAMPTZ,
    deleted_at_utc      TIMESTAMPTZ,
    archived_at_utc     TIMESTAMPTZ,
    interacted_at_utc   TIMESTAMPTZ NOT NULL
);

CREATE TABLE items
(
    entity_id       UUID PRIMARY KEY REFERENCES entities(id)    ON DELETE CASCADE,
    
    state_id        INT NOT NULL REFERENCES item_states(id)     ON DELETE RESTRICT,
    priority_id     INT NOT NULL REFERENCES item_priorities(id) ON DELETE RESTRICT,
    category_id     INT REFERENCES workflow_category_types(id) ON DELETE RESTRICT,
    
    title           TEXT NOT NULL
);

CREATE TABLE relations
(
    entity_id           UUID PRIMARY KEY REFERENCES entities(id) ON DELETE CASCADE,

    entity_parent_id    UUID NOT NULL REFERENCES entities(id) ON DELETE CASCADE,
    entity_child_id     UUID NOT NULL REFERENCES entities(id) ON DELETE CASCADE,
    relation_id         INT  NOT NULL REFERENCES relation_types(id) ON DELETE RESTRICT,
    
    CONSTRAINT ck_no_self_link CHECK (entity_parent_id != entity_child_id),
    CONSTRAINT unique_link UNIQUE (entity_parent_id, entity_child_id, relation_id)
);

CREATE TABLE descriptions
(
    id                  UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id           UUID REFERENCES entities(id) ON DELETE CASCADE,
    type_id             INT  NOT NULL REFERENCES description_types(id) ON DELETE RESTRICT,
    content             TEXT NOT NULL,
    created_at_utc      TIMESTAMPTZ NOT NULL
);
