INSERT INTO action_types (id, name, synopsis)
VALUES
    (100, 'State',    'Change of item state'),
    (101, 'Priority', 'Change of item priority'),
    (102, 'Category', 'Change of item category'),
    (103, 'Title',    'Change of item title');

INSERT INTO session_efficiency_types (name, synopsis, sort_order)
VALUES
    ('Low Output',        'Minimal measurable progress during the session',                1),
    ('Steady Output',     'Consistent work pace with reliable progress',                  2),
    ('High Output',       'Above-average performance with strong progress',               3),
    ('Peak Performance',  'Exceptional productivity and near-optimal workflow',           4);

INSERT INTO session_concentration_types (name, synopsis, sort_order)
VALUES
    ('Distracted',          'Frequent internal or external distractions',                      1),
    ('Unfocused but Calm',  'Low attention but mentally stable and not stressed',              2),
    ('Focused',             'Stable attention and effective cognitive engagement',             3),
    ('Deep Focus',          'Strong concentration with minimal distractibility',               4),
    ('Hyperfocus',          'Intense, near-immersive cognitive engagement',                    5);

INSERT INTO session_states (name, synopsis) 
VALUES
    ('Running',   'Session is currently active and tracking work time'),
    ('Paused',    'Session is temporarily paused; no work time is tracked'),
    ('Completed', 'Session has ended and is permanently closed');
