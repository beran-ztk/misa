CREATE TABLE action_types
(
    id          INT PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    synopsis    TEXT
);

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
)