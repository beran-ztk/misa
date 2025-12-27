CREATE TABLE schedule_frequency_types
(
    id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name      TEXT NOT NULL UNIQUE,
    synopsis  TEXT
);

INSERT INTO schedule_frequency_types (name)
VALUES
    ('Minutes'),
    ('Hours'),
    ('Days'),
    ('Weeks'),
    ('Months'),
    ('Years');

CREATE TABLE schedule
(
    entity_id        UUID PRIMARY KEY REFERENCES entities(id) ON DELETE CASCADE,

    start_at_utc     TIMESTAMPTZ NOT NULL,
    end_at_utc       TIMESTAMPTZ NULL
);

CREATE TABLE schedule_recurrence_rules
(
    id                       UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    schedule_id              UUID NOT NULL REFERENCES schedule(entity_id) ON DELETE CASCADE,

    frequency_type_id        INT NOT NULL REFERENCES schedule_frequency_types(id) ON DELETE RESTRICT,
    frequency_interval       INT NULL,

    occurrence_count_limit   INT NULL,      -- RRULE COUNT: max. insgesamt

    by_day                   INT[] NULL,    -- 1=Mon..7=Sun
    by_month_day             INT[] NULL,    -- 1..31
    by_month                 INT[] NULL,    -- 1..12

    catch_up                 BOOLEAN NOT NULL DEFAULT TRUE,
    max_catch_up_occurrences INT NOT NULL DEFAULT 1,

    time_to_live             INTERVAL NULL, -- TTL pro Occurrence (Expiry)

    is_enabled               BOOLEAN NOT NULL DEFAULT TRUE,

    until_at_utc             TIMESTAMPTZ NULL,
    last_generated_at_utc    TIMESTAMPTZ NULL
);




-- create table calendar_occurrences
-- (
--     id                  uuid primary key default gen_random_uuid(),
--     recurrence_rule_id   uuid not null references schedule_recurrence_rules(id) on delete cascade,
--     
--     occurs_at_utc       timestamptz not null,
--     expires_at_utc      timestamptz null,
--     generated_at_utc    timestamptz not null default now()
-- );
