# Background jobs

`BackgroundJobService` is the single execution boundary for YouTube work. No
service starts `yt-dlp` directly; every invocation goes through
`TrackDownloadService.RunYouTubeProcessAsync` and the shared scheduler.

## Guarantees

- At most three `yt-dlp` processes run at once across imports, Channel Hub,
  metadata reads, playlist previews and direct downloads.
- Jobs are selected by priority and then FIFO within the same priority.
- `UserInitiated` work may overtake queued `Normal` and `Background` work.
- Pausing background work does not block user-initiated or normal work.
- Cancellation reaches the operating-system process and terminates its process
  tree.
- Every execution has a stable ID, origin, state, attempt count, progress text,
  timestamps and a sanitized error suitable for the later Activity Center.
- Retry delay uses exponential backoff when a job opts into multiple attempts.

## Persistence boundary

The scheduler coordinates executions in memory. Durable ownership remains with
the existing domain queues:

- `ImportQueueService` persists import batches and recovers interrupted imports.
- Channel metadata and downloads persist their state in `channel_videos`.
- Channel subscriptions persist the data needed for a later refresh.

Channel metadata `Failed` is terminal for automatic scheduling. Startup
recovery, periodic backfill and automatic channel snapshots preserve the status,
error and attempt count. Only an explicit per-video retry, the channel's retry-
issues action, or a user-triggered channel refresh may reset it to pending. This
keeps age-restricted and similarly inaccessible videos from creating permanent
background traffic.

This separation avoids persisting delegates or duplicating domain state. After a
restart the domain queue recreates the execution job, while the scheduler again
applies the global limit and priority rules.

## Priority mapping

| Origin | Priority |
| --- | --- |
| Add Track, import preview, manual Channel Hub action | `UserInitiated` |
| Persisted import batch | `Normal` |
| Channel enrichment, followed-channel refresh, metadata backfill, auto-download | `Background` |

The Activity Center should consume `GetSnapshot()` and `SnapshotChanged`; it
must not infer job state from individual view flags.

## Activity Center

The toolbar Activity Center is a session view over the scheduler snapshot. It
shows the global `running / 3` YouTube limit, active and queued work, and the 30
most recent finished jobs. Long errors are capped at two lines so actions remain
reachable.

The pause action only prevents new `Background` jobs from starting. Running,
user-initiated and normal jobs continue. Cancel terminates the selected current
execution and its `yt-dlp` process tree. Finished session history can be cleared
without touching the durable import or Channel Hub records that own the actual
workflow.
