using System;
using System.Threading.Tasks;

namespace Resona.Services;

public static class AnalysisRetryPolicy
{
    public const int MaxAttempts = 3;

    public static async Task<bool> CheckConnectionAsync(
        Func<int, Task<bool>> checkAttempt,
        Func<int, Task>? waitAfterFailure = null)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            if (await checkAttempt(attempt))
                return true;

            if (attempt < MaxAttempts && waitAfterFailure is not null)
                await waitAfterFailure(attempt);
        }

        return false;
    }
}
