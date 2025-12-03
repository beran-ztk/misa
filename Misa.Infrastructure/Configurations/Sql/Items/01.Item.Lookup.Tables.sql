CREATE TABLE item_states
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT,
    sort_order  INT NOT NULL UNIQUE
);

CREATE TABLE item_priorities
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT,
    sort_order  INT NOT NULL UNIQUE
);

CREATE TABLE item_relations
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT,
    sort_order  INT NOT NULL UNIQUE
);

CREATE TABLE item_categories
(
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT
);
