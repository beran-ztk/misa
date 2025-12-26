



-- Zettelkasten

create table lu_revisions
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_revisions (name)
VALUES ('Test');

create table lu_weights
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_weights (name)
VALUES ('Test');

create table lu_discretions
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_discretions (name)
VALUES ('Test');

create table lu_tags
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_tags (name)
VALUES ('Meow'),('Evergreen'),('Properties besser'),('Wasser'),('Lampe');

create table lu_properties
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_properties (name)
VALUES ('Created'),('Difficulty'),('Readiness'),('Wew'),('Tür');



-- Topic

create table lu_recalls
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_recalls (name)
VALUES ('Test');

create table lu_comprehensions
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_comprehensions (name)
VALUES ('Test');

create table lu_analyses
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_analyses (name)
VALUES ('Test');

create table lu_applications
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_applications (name)
VALUES ('Test');

create table lu_creations
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_creations (name)
VALUES ('Test');

create table lu_teachabilities
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_teachabilities (name)
VALUES ('Test');

create table lu_confidences
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_confidences (name)
VALUES ('Test');



-- Unit

create table lu_completions
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_completions (name)
VALUES ('Test'),('Completed'),('Above Expactations'),('insanely perfect');

create table lu_attainments
(
    id       int generated always as identity primary key,
    name     text not null unique,
    synopsis text
);
INSERT INTO lu_attainments (name)
VALUES ('Test');
