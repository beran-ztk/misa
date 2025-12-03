CREATE EXTENSION IF NOT EXISTS "pgcrypto"; -- For gen_random_uuid() function

-- create table items
-- (
    -- id            int generated always as identity (start with 100000) primary key,
    -- fk_state      int references lu_states(id) on delete restrict,
    -- fk_priority   int references lu_priorities(id) on delete restrict,
    -- fk_category   int references lu_categories(id) on delete restrict,
    -- name          text not null,
    -- note          text,
    -- ts_created    timestamptz default now()
-- );

CREATE TABLE items
(
    id              UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    title           TEXT NOT NULL,
    description     TEXT,
    created_at_utc  TIMESTAMPTZ DEFAULT now()
);


create table item_relations
(
    id              int generated always as identity primary key,
    fk_item_parent  int references items(id) on delete restrict,
    fk_item_child   int references items(id) on delete restrict,
    fk_relation     int references lu_relations(id) on delete restrict,
    
    constraint unique_link unique (fk_item_parent, fk_item_child, fk_relation),
    constraint no_self_link check (fk_item_parent != fk_item_child)
);

create table item_modules
(
    id              int generated always as identity primary key,
    fk_item         int not null references items(id) on delete restrict,
    fk_completion   int references lu_completions(id) on delete restrict, -- Wie viel vom geplanten
    fk_attainment   int references lu_attainments(id) on delete restrict, -- Verständnisgrad der neuen Inhalte
    purpose         text,
    summary         text
);