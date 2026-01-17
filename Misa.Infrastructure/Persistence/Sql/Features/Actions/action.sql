CREATE TABLE action_types
(
    id          INT PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT
);

CREATE TABLE actions
(
    id              UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id       UUID REFERENCES entities(id) ON DELETE CASCADE,

    type_id         INT  NOT NULL REFERENCES action_types(id) ON DELETE RESTRICT,

    value_before    TEXT,
    value_after     TEXT,

    reason          TEXT,

    created_at_utc      TIMESTAMPTZ NOT NULL
);

INSERT INTO action_types (id, name, synopsis)
VALUES
    (100, 'State',    'Change of item state'),
    (101, 'Priority', 'Change of item priority'),
    (102, 'Category', 'Change of item category'),
    (103, 'Title',    'Change of item title'),
    (104, 'Deadline', 'Change of item deadline');
