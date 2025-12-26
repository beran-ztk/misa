INSERT INTO item_states (id, name, synopsis, sort_order)
VALUES
-- Base
(1,  'Draft',     'Entwurf; noch nie daran gearbeitet',                         1),
(2,  'Undefined', 'Unklar; muss präzisiert werden',                             2),
(3,  'Scheduled', 'Geplant für einen zukünftigen Zeitpunkt',                   3),

-- Work
(4,  'InProgress','Bereits bearbeitet, aktuell keine aktive Session',           4),
(5,  'Active',    'Aktive Session läuft',                                       5),
(6,  'Paused',    'Session pausiert (max. 6h, danach Auto-Fortsetzung)',        6),
(7,  'Pending',   'Zurückgestellt; lange nicht bearbeitet',                     7),

-- Blocked
(8,  'WaitForResponse',      'Wartet auf Rückmeldung einer Person oder Stelle',  8),
(9,  'BlockedByRelationship','Blockiert durch Relation oder Abhängigkeit',       9),

-- Final
(10, 'Done',      'Erfolgreich abgeschlossen',                                 10),
(11, 'Canceled',  'Abgebrochen; nicht weiter erforderlich',                    11),
(12, 'Failed',    'Gescheitert; Ziel nicht erreicht',                           12),
(13, 'Expired',   'Automatisch abgelaufen (Deadline überschritten)',            13);


INSERT INTO item_priorities (name, synopsis, sort_order)
VALUES
    ('None',     'Keine Priorität vergeben',      1),
    ('Low',      'Geringe Wichtigkeit',           2),
    ('Medium',   'Normale Priorität',             3),
    ('High',     'Wichtig; zeitnah bearbeiten',   4),
    ('Urgent',   'Dringend; sofortige Aktion',    5),
    ('Critical', 'Kritische Eskalation',          6);

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

INSERT INTO workflow_category_types (workflow_id, name, synopsis, sort_order)
VALUES
    (1, 'Work',        'Berufliche Aufgaben, IT-Projekte, Ausbildung im Betrieb',  1),
    (1, 'School',      'Berufsschule, Klausuren, Lernaufgaben, Abgaben',           2),
    (1, 'Personal',    'Private Aufgaben, Haushalt, persönliche Organisation',     3),
    
    (2, 'Deadline', 'Deadline eines Items',  1);


INSERT INTO item_relations_types (name, synopsis, sort_order)
VALUES
    ('contains',  'Parent-Item beinhaltet das Child-Item',                      1),
    ('follows',   'Child folgt logisch oder zeitlich nach Parent',              2),
    ('triggers',  'Parent löst das Child-Item aus (z.B. Event → Action)',       3),
    ('blocks',    'Parent verhindert die Ausführung des Child-Items',           4),
    ('session_of','Child ist eine Session von Parent',                          5);

INSERT INTO description_types (name, synopsis, sort_order)
VALUES
    ('Text',            'Einfacher Fließtext ohne Formatierung',                               1),
    ('Markdown',        'Formatierter Text mit Überschriften, Listen und Hervorhebungen',      2),
    ('Url',             'Referenz auf externe Inhalte, Webseiten oder Dateien',                3),
    ('Picture',         'Grafiken, Screenshots oder eingebettete Bilder',                      4);

