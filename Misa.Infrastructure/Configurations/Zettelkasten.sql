create table topics
(
    id                  int generated always as identity primary key,
    fk_item             int not null references items(id) on delete restrict, -- Ist Item und Beziehung mit Item
    fk_recall           int references lu_recalls(id) on delete restrict,
    fk_comprehension    int references lu_comprehensions(id) on delete restrict,
    fk_analysis         int references lu_analyses(id) on delete restrict,
    fk_application      int references lu_applications(id) on delete restrict,
    fk_creation         int references lu_creations(id) on delete restrict,
    fk_teachability     int references lu_teachabilities(id) on delete restrict,
    fk_confidence       int references lu_confidences(id) on delete restrict
);

create table zettelkasten
(
    id                 int generated always as identity primary key,
    fk_item            int not null references items(id) on delete restrict,
    fk_item_topic      int references items(id) on delete restrict,
    fk_revision        int references lu_revisions(id) on delete restrict,
    fk_weight          int references lu_weights(id) on delete restrict,
    fk_discretion      int references lu_discretions(id) on delete restrict,
    synopsis           text
);

create table zettel_tags
(
    id           int generated always as identity primary key,
    fk_item      int not null references items(id) on delete restrict,
    fk_tag       int not null references lu_tags(id) on delete restrict,
    ts_created   timestamptz default now()
);

create table zettel_properties
(
    id           int generated always as identity primary key,
    fk_item      int not null references items(id) on delete restrict,
    fk_property  int not null references lu_properties(id) on delete restrict,
    value        text not null,
    ts_created   timestamptz default now()
);