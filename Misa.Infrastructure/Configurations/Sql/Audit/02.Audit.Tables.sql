CREATE TABLE audit_actions
(
    entity_id       UUID PRIMARY KEY REFERENCES entities(id) ON DELETE CASCADE,
    
    type_id         INT  NOT NULL REFERENCES audit_action_types(id) ON DELETE RESTRICT,
    
    value_before    TEXT,
    value_after     TEXT
);

CREATE TABLE audit_sessions
(
    entity_id           UUID PRIMARY KEY REFERENCES entities(id) ON DELETE CASCADE,
    
    efficiency_id       INT REFERENCES audit_session_efficiency_types(id) ON DELETE RESTRICT,
    concentration_id    INT REFERENCES audit_session_concentration_types(id) ON DELETE RESTRICT,
    
    planned_duration    INTERVAL,
    
    stop_automatically  BOOL NOT NULL DEFAULT FALSE,
    
    started_at_utc      TIMESTAMPTZ NOT NULL,
    ended_at_utc        TIMESTAMPTZ,

    CONSTRAINT ck_ended_after_started CHECK (ended_at_utc IS NULL OR started_at_utc <= ended_at_utc),
    CONSTRAINT ck_planned_duration 
        CHECK (planned_duration IS NULL 
                   OR (planned_duration > INTERVAL '0 seconds' AND planned_duration < INTERVAL '1 day')
);
