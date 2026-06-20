Database schema draft

Naming convention:
- Table names: plural, snake_case
- Column names: snake_case
- Primary key column: id
- Foreign key columns: <referenced_singular>_id
- Date/time columns: *_at
- Boolean columns: is_* / has_* / needs_*
- Duration values in seconds
- Scores as decimal/real values between 0 and 1 unless stated otherwise


1. tracks

Purpose:
Stores the local music tracks managed by the application.

Columns:
- id                       primary key
- channel_id               foreign key -> channels.id, nullable
- rating_id                foreign key -> ratings.id, nullable

- canonical_url            original source URL, unique if available
- title                    display title
- file_name                local file name or relative file path

- duration_seconds         track duration in seconds
- uploaded_at              source upload date/time, nullable
- downloaded_at            local download date/time
- updated_at               last local metadata update

- listen_count             number of completed/started listens
- skip_count               number of skips
- last_listened_at         nullable

- needs_reevaluation       boolean flag for manual review or re-analysis
- notes                    free text notes


2. channels

Purpose:
Stores source channels, for example YouTube channels.

Columns:
- id                       primary key
- name                     channel display name
- source_channel_id        external channel id, nullable, unique if available
- source_url               channel URL, nullable
- inform_new_songs         boolean flag


3. ratings

Purpose:
Stores fixed rating levels used by tracks.

Columns:
- id                       primary key
- name                     rating name
- sort_order               display/order value


4. genres

Purpose:
Stores the application’s own final genre list.

Columns:
- id                       primary key
- key                      stable internal key, unique
- name                     display name
- description              nullable
- sort_order               display/order value
- is_enabled               boolean flag


5. track_genres

Purpose:
Stores the final genres assigned to a track. This is the application truth, not just a model suggestion.

Columns:
- track_id                 foreign key -> tracks.id
- genre_id                 foreign key -> genres.id
- set_by_source_id         foreign key -> genre_assignment_sources.id
- assigned_at              date/time of assignment

Primary key / unique:
- unique(track_id, genre_id)


6. genre_assignment_sources

Purpose:
Stores where a final genre assignment came from.

Example values:
- manual
- model_suggestion
- import
- system

Columns:
- id                       primary key
- key                      stable internal key, unique
- name                     display name


7. track_analysis

Purpose:
Stores one analysis result for a track. Currently there is only one analyzer/model, so no separate models table is needed.

Columns:
- id                       primary key
- track_id                 foreign key -> tracks.id, unique

- analyzed_at              when the analysis was created
- analyzer_name            optional text, e.g. "discogs-maest"
- analyzer_version         optional text

- bpm                      nullable numeric value
- integrated_loudness      nullable numeric value in LUFS
- loudness_range           nullable numeric value in LU
- danceability             nullable numeric value

Notes:
- Additional stable analysis values can be added later as normal columns.
- This table represents the analysis run/result for one track.


8. track_genre_predictions

Purpose:
Stores the genre/subgenre scores produced by the analyzer for a track analysis.

Columns:
- id                       primary key
- track_analysis_id        foreign key -> track_analysis.id
- model_subgenre_id        foreign key -> model_subgenres.id
- score                    numeric prediction score

Primary key / unique:
- unique(track_analysis_id, model_subgenre_id)


9. model_genres

Purpose:
Stores the genre categories known by the analyzer/model.

Columns:
- id                       primary key
- name                     model genre name, unique


10. model_subgenres

Purpose:
Stores the subgenre labels known by the analyzer/model.

Columns:
- id                       primary key
- model_genre_id           foreign key -> model_genres.id
- name                     model subgenre name

Primary key / unique:
- unique(model_genre_id, name)


11. genre_mappings

Purpose:
Maps analyzer/model subgenres to the application’s own genres.

Columns:
- id                       primary key
- genre_id                 foreign key -> genres.id
- model_subgenre_id        foreign key -> model_subgenres.id

Primary key / unique:
- unique(genre_id, model_subgenre_id)

Notes:
- One model subgenre may map to multiple application genres.
- Unmapped model subgenres should not be forced into the application genre system.
- High-scoring unmapped labels can be displayed as informational model output.


Important conceptual rules:

- tracks are the core entity.
- genres are the final application genres.
- track_genres are final accepted genre assignments.
- track_analysis stores computed audio analysis values for a track.
- track_genre_predictions stores raw model subgenre scores.
- genre_mappings translate model subgenres into application genres.
- model_genres and model_subgenres are static reference data from the analyzer/model.
- ratings and genre_assignment_sources are static reference data.
- styles are intentionally removed for the first stable version.
- a separate models table is intentionally not used for now.
