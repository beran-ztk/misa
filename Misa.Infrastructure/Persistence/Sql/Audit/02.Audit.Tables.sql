CREATE TABLE actions
(
    id              UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id       UUID REFERENCES entities(id) ON DELETE CASCADE,
    
    type_id         INT  NOT NULL REFERENCES action_types(id) ON DELETE RESTRICT,
    
    value_before    TEXT,
    value_after     TEXT,
    
    reason          TEXT,
    
    created_at_utc      TIMESTAMPTZ NOT NULL
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

