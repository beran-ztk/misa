create table session_efficiency_types
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT,
    sort_order  INT NOT NULL UNIQUE
);

create table session_concentration_types
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT,
    sort_order  INT NOT NULL UNIQUE
);

CREATE TABLE session_states
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT
);


CREATE TABLE sessions
(
    id                  UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    item_id             UUID NOT NULL REFERENCES items(entity_id) ON DELETE CASCADE,

    state_id            INT NOT NULL REFERENCES session_states(id) ON DELETE RESTRICT,
    efficiency_id       INT REFERENCES session_efficiency_types(id) ON DELETE RESTRICT,
    concentration_id    INT REFERENCES session_concentration_types(id) ON DELETE RESTRICT,

    objective           TEXT,
    summary             TEXT,
    auto_stop_reason    TEXT,

    planned_duration    INTERVAL,

    stop_automatically  BOOL NOT NULL DEFAULT FALSE,
    was_automatically_stopped BOOL,

    created_at_utc      TIMESTAMPTZ DEFAULT now(),

    CONSTRAINT ck_planned_duration
        CHECK (planned_duration IS NULL
            OR (planned_duration > INTERVAL '0 seconds' AND planned_duration < INTERVAL '1 day'))
);

CREATE TABLE session_segments
(
    id                  UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    session_id          UUID REFERENCES sessions(id) ON DELETE CASCADE,

    pause_reason        TEXT,

    started_at_utc      TIMESTAMPTZ NOT NULL,
    ended_at_utc        TIMESTAMPTZ,

    CONSTRAINT ck_ended_after_started CHECK (ended_at_utc IS NULL OR started_at_utc <= ended_at_utc)
);


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
