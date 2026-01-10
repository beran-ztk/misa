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

CREATE TABLE entity_workflow_types
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT
);

CREATE TABLE workflow_category_types
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    workflow_id INT NOT NULL REFERENCES entity_workflow_types(id) ON DELETE RESTRICT,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT,
    sort_order  INT NOT NULL UNIQUE
);

CREATE TABLE relation_types
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT
);