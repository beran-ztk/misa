using System;

namespace Resona.Services;

public static class LoudnessNormalizer
{
    private const double TargetLufs = -14.0;
    private const double MinimumGainDb = -10.0;
    private const double MaximumGainDb = 6.0;

    public static float CalculateGain(double? integratedLoudness, double? loudnessRange)
    {
        if (integratedLoudness is not double lufs || !double.IsFinite(lufs))
            return 1f;

        var gainDb = TargetLufs - lufs;
        if (gainDb > 0 && loudnessRange is double range && double.IsFinite(range))
        {
            if (range >= 16)
                gainDb *= 0.55;
            else if (range >= 10)
                gainDb *= 0.75;
        }

        gainDb = Math.Clamp(gainDb, MinimumGainDb, MaximumGainDb);
        return (float)Math.Pow(10.0, gainDb / 20.0);
    }
}
