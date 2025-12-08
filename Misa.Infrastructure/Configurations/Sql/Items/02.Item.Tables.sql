CREATE TABLE entities
(
    id                  UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_id            UUID,
    workflow_id         INT NOT NULL REFERENCES entity_workflow_types(id) ON DELETE RESTRICT,
    
    is_deleted          BOOL NOT NULL DEFAULT FALSE,
    
    created_at_utc      TIMESTAMPTZ NOT NULL,
    updated_at_utc      TIMESTAMPTZ,
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
CREATE INDEX idx_items_state
    ON items(state_id);
CREATE INDEX idx_items_priority
    ON items(priority_id);

CREATE TABLE relations
(
    entity_id           UUID PRIMARY KEY REFERENCES entities(id) ON DELETE CASCADE,

    entity_parent_id    UUID NOT NULL REFERENCES entities(id) ON DELETE CASCADE,
    entity_child_id     UUID NOT NULL REFERENCES entities(id) ON DELETE CASCADE,
    relation_id         INT  NOT NULL REFERENCES item_relations_types(id) ON DELETE RESTRICT,

    sort_order          INT
    
    CONSTRAINT ck_no_self_link CHECK (entity_parent_id != entity_child_id),
    CONSTRAINT unique_link UNIQUE (entity_parent_id, entity_child_id, relation_id),
    CONSTRAINT unique_sort_order UNIQUE (entity_parent_id, relation_id, sort_order)
);
CREATE INDEX idx_relations_parent 
    ON relations(entity_parent_id);
CREATE INDEX idx_relations_child
    ON relations(entity_child_id);
CREATE INDEX idx_relations_relation
    ON relations(relation_id);

CREATE TABLE descriptions
(
    entity_id       UUID PRIMARY KEY REFERENCES entities(id) ON DELETE CASCADE,
    type_id         INT  NOT NULL REFERENCES description_types(id) ON DELETE RESTRICT,
    content         TEXT NOT NULL,
);
