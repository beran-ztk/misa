CREATE TABLE item_states
(
    id          INT PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT,
    sort_order  INT NOT NULL UNIQUE
);
CREATE TABLE item_priorities
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT,
    sort_order  INT NOT NULL UNIQUE
);
CREATE TABLE workflow_category_types
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    workflow_id INT NOT NULL REFERENCES entity_workflow_types(id) ON DELETE RESTRICT,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT,
    sort_order  INT NOT NULL UNIQUE
);
CREATE TABLE items
(
    entity_id       UUID PRIMARY KEY REFERENCES entities(id)    ON DELETE CASCADE,

    state_id        INT NOT NULL REFERENCES item_states(id)     ON DELETE RESTRICT,
    priority_id     INT NOT NULL REFERENCES item_priorities(id) ON DELETE RESTRICT,
    category_id     INT REFERENCES workflow_category_types(id) ON DELETE RESTRICT,

    title           TEXT NOT NULL
);
