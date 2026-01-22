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
INSERT INTO entity_workflow_types (name, synopsis)
VALUES
    ('Task',         'Konkrete Aufgaben oder To-Dos'),
    ('Schedule',     'Kalendereintrag, Termin oder zeitliche Planung'),
    ('Event',        'Ereignis mit festem Zeitpunkt'),
    ('Notification', 'Benachrichtigung oder Reminder'),
    ('Journal',      'Persönliche oder berufliche Notizen'),
    ('Module',       'Übergeordnetes Lern- oder Projektmodul'),
    ('Unit',         'Einzelne Lerneinheit oder Arbeitsschritt'),
    ('Session',      'Aktive Sitzung'),
    ('Description',  'Unterschiedliche Zusatzinformationen für Entitäten');
