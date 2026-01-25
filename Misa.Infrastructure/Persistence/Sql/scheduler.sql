CREATE OR REPLACE FUNCTION array_all_between(
    arr int[],
    min_val int,
    max_val int
)
RETURNS boolean
LANGUAGE sql
IMMUTABLE
AS $$
SELECT NOT EXISTS (
    SELECT 1
    FROM unnest(arr) v
    WHERE v < min_val OR v > max_val
);
$$;

DROP TABLE IF EXISTS scheduler_execution_log;
DROP TABLE IF EXISTS scheduler;
DROP TYPE IF EXISTS scheduler_frequency_type;
DROP TYPE IF EXISTS scheduler_misfire_policy;
DROP TYPE IF EXISTS scheduler_execution_status;

CREATE TYPE scheduler_misfire_policy AS ENUM ('catchup', 'skip', 'runOnce');

CREATE TYPE scheduler_execution_status AS ENUM
(
    'pending',      -- Fälligkeit ist registriert, aber noch nicht von einem Runner beansprucht
    'claimed',      -- Ein Runner hat die Fälligkeit reserviert
    'running',      -- Ausführung läuft
    'succeeded',    -- Erfolgreich abgeschlossen
    'failed',       -- Fehler, kann evtl. retryt werden
    'skipped'       -- Bewusst nicht ausgeführt (z. B. misfire=skip)
);

CREATE TYPE scheduler_frequency_type AS ENUM 
(
    'once',
    'minutes',
    'hours',
    'days',
    'weeks',
    'months',
    'years'
);

CREATE TABLE scheduler
(
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    item_id                 UUID NOT NULL REFERENCES items(entity_id) ON DELETE CASCADE,

    frequency_type_id       INT  NOT NULL REFERENCES scheduler_frequency_types(id) ON DELETE RESTRICT,
    frequency_interval      INT  NOT NULL DEFAULT 1 CHECK (frequency_interval >= 1),

    occurrence_count_limit  INT  NULL,

    by_day                  INT[] NULL, -- 1=Mon..7=Sun
    by_month_day            INT[] NULL, -- 1..31
    by_month                INT[] NULL, -- 1..12

    -- Regel für verpassten Trigger
    misfire_policy          scheduler_misfire_policy NOT NULL DEFAULT 'catchup',

    lookahead_count         INT NOT NULL DEFAULT 1 CHECK (lookahead_count >= 0), -- zukünftige Triggers

    -- Notification/Tasks/Re-Try - läuft nach diesem Zeitpunkt ab
    occurrence_ttl          INTERVAL NULL CHECK (occurrence_ttl IS NULL OR occurrence_ttl > INTERVAL '0'),

    payload                JSONB NULL,

    timezone               TEXT NOT NULL CHECK (timezone <> ''),

    -- Trigger NUR in diesem zeitlichen Zeitraum
    start_time             TIME NULL,
    end_time               TIME NULL,

    -- Trigger NUR in diesem Datumszeitraum
    active_from_utc        TIMESTAMPTZ NOT NULL,
    active_until_utc       TIMESTAMPTZ NULL,

    last_run_at_utc        TIMESTAMPTZ NULL, -- Letzter Trigger-Zeitpunkt
    next_due_at_utc        TIMESTAMPTZ NULL, -- Nächster Trigger-Zeitpunkt
    
    locked_by               TEXT NULL,
    locked_until_utc        TIMESTAMPTZ NULL,
    
    last_error              TEXT NULL,
    last_error_at_utc       TIMESTAMPTZ NULL,

    CHECK (
        (start_time IS NULL AND end_time IS NULL)
            OR (start_time IS NOT NULL AND end_time IS NOT NULL AND start_time < end_time)
        ),
    CHECK (active_until_utc IS NULL OR active_until_utc > active_from_utc),
    CHECK (occurrence_count_limit IS NULL OR occurrence_count_limit >= 1),
    CHECK (next_due_at_utc IS NULL OR last_run_at_utc IS NULL OR next_due_at_utc >= last_run_at_utc),

    CHECK (by_day IS NULL OR array_all_between(by_day, 1, 7)),
    CHECK (by_month_day IS NULL OR array_all_between(by_month_day, 1, 31)),
    CHECK (by_month IS NULL OR array_all_between(by_month, 1, 12))
);

CREATE TABLE scheduler_execution_log
(
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scheduler_id        UUID NOT NULL REFERENCES scheduler(id) ON DELETE CASCADE,

    scheduled_for_utc   TIMESTAMPTZ NOT NULL, -- Triggerzeitpunkt

    claimed_at_utc      TIMESTAMPTZ NULL, -- Vom Runner beansprucht
    started_at_utc      TIMESTAMPTZ NULL, -- Ausführungszeitpunkt
    finished_at_utc     TIMESTAMPTZ NULL, -- Endzeitpunkt

    status              scheduler_execution_status NOT NULL DEFAULT 'pending',
    error               TEXT NULL,
    attempts            INT NOT NULL DEFAULT 0 CHECK (attempts >= 0), -- Wiederholte Versuche

    created_at_utc      TIMESTAMPTZ NOT NULL DEFAULT now(),

    

    CHECK (started_at_utc IS NULL OR claimed_at_utc <= started_at_utc),
    CHECK (finished_at_utc IS NULL OR started_at_utc <= finished_at_utc),

    CHECK (status <> 'pending' OR (claimed_at_utc IS NULL AND started_at_utc IS NULL AND finished_at_utc IS NULL)),

    CHECK (status = 'pending' OR claimed_at_utc IS NOT NULL),
    CHECK (status IN ('pending','claimed') OR started_at_utc IS NOT NULL),
    CHECK (status NOT IN ('succeeded','failed','skipped') OR finished_at_utc IS NOT NULL)
);
