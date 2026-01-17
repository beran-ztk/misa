CREATE TABLE entity_workflow_types
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT
);

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

INSERT INTO entity_workflow_types (workflow_id, name, synopsis, sort_order)
VALUES
    (1, 'Work',        'Berufliche Aufgaben, IT-Projekte, Ausbildung im Betrieb',  1),
    (1, 'School',      'Berufsschule, Klausuren, Lernaufgaben, Abgaben',           2),
    (1, 'Personal',    'Private Aufgaben, Haushalt, persönliche Organisation',     3),

    (2, 'Deadline', 'Deadline eines Items',  1);
