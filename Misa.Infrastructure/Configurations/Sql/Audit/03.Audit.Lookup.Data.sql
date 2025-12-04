INSERT INTO audit_action_types (name, synopsis)
VALUES
    ('Deleted',  'Delete an entity'),
    ('State',    'Change of item state'),
    ('Priority', 'Change of item priority'),
    ('Title',    'Change of item title');

INSERT INTO audit_session_efficiency_types (name, synopsis, sort_order)
VALUES
    ('Low Output',        'Minimal measurable progress during the session',                1),
    ('Steady Output',     'Consistent work pace with reliable progress',                  2),
    ('High Output',       'Above-average performance with strong progress',               3),
    ('Peak Performance',  'Exceptional productivity and near-optimal workflow',           4);

INSERT INTO audit_session_concentration_types (name, synopsis, sort_order)
VALUES
    ('Distracted',          'Frequent internal or external distractions',                      1),
    ('Unfocused but Calm',  'Low attention but mentally stable and not stressed',              2),
    ('Focused',             'Stable attention and effective cognitive engagement',             3),
    ('Deep Focus',          'Strong concentration with minimal distractibility',               4),
    ('Hyperfocus',          'Intense, near-immersive cognitive engagement',                    5);
