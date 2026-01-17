CREATE TABLE relation_types
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT
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
INSERT INTO relation_types (name, synopsis)
VALUES
    ('has_deadline',  'Parent-Item hat eine Deadline'),

    ('contains',  'Parent-Item beinhaltet das Child-Item'),
    ('follows',   'Child folgt logisch oder zeitlich nach Parent'),
    ('triggers',  'Parent löst das Child-Item aus (z.B. Event → Action)'),
    ('blocks',    'Parent verhindert die Ausführung des Child-Items'),
    ('session_of','Child ist eine Session von Parent');
