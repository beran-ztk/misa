create table contacts
(
    id           int generated always as identity primary key,
    name         text not null,
    surname      text not null,
    birthday     date,
    ts_created   timestamptz DEFAULT now(),
    ts_updated   timestamptz
);

create table contact_addresses
(
    id           int generated always as identity primary key,
    fk_contact   int not null references contacts(id) on delete restrict,
    label        text,
    street       text,
    house_no     text,
    sub_address  text,
    zip_code     text,
    city         text,
    country      text default 'DE',
    is_primary   boolean default true,
    ts_created   timestamptz DEFAULT now()
);

create table contact_channels
(
    id           int generated always as identity primary key,
    fk_contact   int not null references contacts(id) on delete restrict,
    fk_category  int not null references lu_categories(id) on delete restrict,
    value        text not null,
    note         text,
    ts_created   timestamptz default now()
);