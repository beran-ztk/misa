using Resona.Models;

namespace Resona.Services;

/// <summary>Documents and validates persisted import-queue transitions.</summary>
public static class ImportQueueStateMachine
{
    public static bool CanTransition(ImportQueueStatus from, ImportQueueStatus to)
    {
        if (from == to)
            return true; // progress text may be updated without changing phase

        return (from, to) switch
        {
            (ImportQueueStatus.Queued, ImportQueueStatus.Downloading) => true,
            (ImportQueueStatus.Queued, ImportQueueStatus.Failed) => true,
            (ImportQueueStatus.Queued, ImportQueueStatus.Skipped) => true,
            (ImportQueueStatus.Downloading, ImportQueueStatus.Analyzing) => true,
            (ImportQueueStatus.Downloading, ImportQueueStatus.ReadyForReview) => true,
            (ImportQueueStatus.Downloading, ImportQueueStatus.Failed) => true,
            (ImportQueueStatus.Analyzing, ImportQueueStatus.ReadyForReview) => true,
            (ImportQueueStatus.Analyzing, ImportQueueStatus.Failed) => true,
            (ImportQueueStatus.Failed, ImportQueueStatus.Queued) => true,
            _ => false
        };
    }
}
