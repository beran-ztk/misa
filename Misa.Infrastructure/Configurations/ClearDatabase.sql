begin;
drop table if exists audit_sessions;
drop table if exists audit_actions;

drop table if exists item_modules;
drop table if exists item_relations;
drop table if exists zettel_tags;
drop table if exists zettel_properties;
drop table if exists zettelkasten;
drop table if exists topics;

drop table if exists events;
drop table if exists calendar;
drop table if exists items;
drop table if exists contact_channels;
drop table if exists contact_addresses;
drop table if exists contacts;

drop table if exists lu_categories;
drop table if exists lu_states;
drop table if exists lu_recurrings;
drop table if exists lu_priorities;

drop table if exists lu_revisions;
drop table if exists lu_weights;
drop table if exists lu_discretions;
drop table if exists lu_tags;
drop table if exists lu_properties;

drop table if exists lu_efficiencies;
drop table if exists lu_concentrations;
drop table if exists lu_actions;
drop table if exists lu_relations;

drop table if exists lu_recalls;
drop table if exists lu_comprehensions;
drop table if exists lu_analyses;
drop table if exists lu_applications;
drop table if exists lu_creations;
drop table if exists lu_teachabilities;
drop table if exists lu_confidences;

drop table if exists lu_completions;
drop table if exists lu_attainments;
commit;