CREATE TABLE scheduled_deadlines
(
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    item_id         UUID NOT NULL REFERENCES items(entity_id) ON DELETE CASCADE,

    deadline_at_utc TIMESTAMPTZ NOT NULL,

    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT uq_scheduled_deadlines_item UNIQUE (item_id)
);