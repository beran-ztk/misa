INSERT INTO item_states (name, synopsis, sort_order)
VALUES
    ('Draft',     'Entwurf; noch nicht freigegeben',                    1),
    ('Open',      'Offen; bereit zur Bearbeitung',                      2),
    ('Active',    'In Bearbeitung; aktueller Fokus',                    3),
    ('Paused',    'Kurzfristig gestoppt; jederzeit fortsetzbar',        4),
    ('Blocked',   'Blockiert durch externe Abhängigkeiten',             5),
    ('Done',      'Erledigt und abgeschlossen',                         6),
    ('Canceled',  'Abgebrochen; wird nicht mehr bearbeitet',            7),
    ('Failed',    'Gescheitert; Ziel wurde nicht erreicht',             8),
    ('Archived',  'Aus dem aktiven Bereich ausgelagert',                9),
    ('Removed',   'Entfernt; praktisch gelöscht',                      10),
    ('Expired',   'Abgelaufen; nicht rechtzeitig abgeschlossen',       11);

INSERT INTO item_priorities (name, synopsis, sort_order)
VALUES
    ('None',     'Keine Priorität vergeben',      1),
    ('Low',      'Geringe Wichtigkeit',           2),
    ('Medium',   'Normale Priorität',             3),
    ('High',     'Wichtig; zeitnah bearbeiten',   4),
    ('Urgent',   'Dringend; sofortige Aktion',    5),
    ('Critical', 'Kritische Eskalation',          6);

INSERT INTO item_relations (name, synopsis, sort_order)
VALUES
    ('contains',  'Parent-Item beinhaltet das Child-Item',                           1),
    ('follows',   'Child folgt logisch oder zeitlich nach Parent',                   2),
    ('triggers',  'Parent löst das Child-Item aus (z.B. Event → Action)',            3),
    ('blocks',    'Parent verhindert oder verzögert die Ausführung des Child-Items', 4);

INSERT INTO item_categories (name, synopsis)
VALUES
    ('Task',         'Konkrete Aufgaben oder To-Dos'),
    ('Calendar',     'Kalendereintrag, Termin oder zeitliche Planung'),
    ('Event',        'Ereignis mit festem Zeitpunkt'),
    ('Notification', 'Benachrichtigung oder Reminder'),
    ('Journal',      'Persönliche oder berufliche Notizen'),
    ('Module',       'Übergeordnetes Lern- oder Projektmodul'),
    ('Unit',         'Einzelne Lerneinheit oder Arbeitsschritt');


-- CREATE UNIQUE INDEX ux_item_categories_name
--     ON item_categories((id/100), name);
-- CREATE UNIQUE INDEX ux_item_categories_sort_order
--     ON item_categories((id/100), sort_order);

-- INSERT INTO item_categories (id, name, synopsis, sort_order)
-- VALUES
--     (100, 'Personal',       'Private oder persönliche Aufgaben',            1),
--     (101, 'School',         'Aufgaben aus Ausbildung oder Studium',         2),
--     (102, 'Work',           'Berufliche Aufgaben',                          3),
-- 
--     (200, 'Calendar',       'Kalendereintrag oder Termin',                  1),
--     (201, 'Event',          'Kalenderereignis',                             2),
--     (203, 'Module',         'Moduleintrag',                                 3),
--     (204, 'Notification',   'Benachrichtigung',                             4),
-- 
--     (300, 'General',        'Allgemeiner Tagebucheintrag',                  1),
--     (301, 'Personal',       'Persönliche Ereignisse und Gedanken',          2),
--     (302, 'Work',           'Berufliche Ereignisse oder Reflexionen',       3),
--     (304, 'School',         'Schulische Ereignisse und Gedanken',           4),
-- 
--     (400, 'Task',           'Einfache oder neutrale Aufgabe im Lernkontext',                1),
--     (401, 'Read',           'Theorie, Artikel oder Dokumentation lesen',                    2),
--     (402, 'Watch',          'Videos, Vorträge oder Präsentationen anschauen',               3),
--     (403, 'Analysis',       'Beispiele, Fehlerquellen oder Konzepte analysieren',           4),
--     (404, 'Summarize',      'Definitionen, Zusammenfassungen oder Glossare erstellen',      5),
--     (405, 'Brainstorm',     'Ideen sammeln, Fragen erzeugen oder Ansätze entwickeln',       6),
--     (406, 'Exploration',    'Freies Lernen ohne Ziel, neue Ideen und Wege entdecken',       7),
--     (407, 'Review',         'Wissen wiederholen, testen oder Feedback einholen',            8),
--     (408, 'Output',         'Ergebnisse produzieren, erklären, veröffentlichen, testen',    9),
--     (409, 'Implementation', 'Praktisch umsetzen oder programmieren',                        10);

