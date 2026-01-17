INSERT INTO description_types (name, synopsis, sort_order)
VALUES
    ('Text',            'Einfacher Fließtext ohne Formatierung'),
    ('Markdown',        'Formatierter Text mit Überschriften, Listen und Hervorhebungen'),
    ('Url',             'Referenz auf externe Inhalte, Webseiten oder Dateien'),
    ('Picture',         'Grafiken, Screenshots oder eingebettete Bilder');

CREATE TABLE descriptions
(
    id                  UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id           UUID REFERENCES entities(id) ON DELETE CASCADE,
    content             TEXT NOT NULL,
    created_at_utc      TIMESTAMPTZ NOT NULL DEFAULT now()
);