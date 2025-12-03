create table calendar
(
    id                   int generated always as identity primary key,
    fk_item              int references items(id) on delete restrict,
    fk_recurring         int references lu_recurrings(id) on delete restrict,
    interval             int,
    occurrence           int,
    instance_ttl_hours   int,
    is_catch_up          boolean,
    is_active            boolean,
    ts_started           timestamptz,
    ts_until             timestamptz,
    ts_last_generated    timestamptz
);

create table events
(
    id                  int generated always as identity primary key,
    fk_item             int not null references items(id) on delete restrict,
    fk_item_calendar    int not null references items(id) on delete restrict,
    ts_scheduled        timestamptz NOT NULL,
    ts_expires          timestamptz
);
