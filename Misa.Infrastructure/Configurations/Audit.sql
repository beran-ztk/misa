create table audit_sessions
(
    id                 int generated always as identity primary key,
    fk_item            int not null references items(id) on delete restrict,
    fk_efficiency      int references lu_efficiencies(id) on delete restrict,
    fk_concentration   int references lu_concentrations(id) on delete restrict,
    objective          text,
    synopsis           text,
    planned_duration   int,
    ts_started         timestamptz default now(),
    ts_ended           timestamptz
);

create table audit_actions
(
    id              int generated always as identity primary key,
    fk_item         int not null references items(id) on delete restrict,
    fk_action       int not null references lu_actions(id) on delete restrict,
    value_before    text,
    value_after     text,
    reason          text,
    ts_created      timestamptz default now()
);