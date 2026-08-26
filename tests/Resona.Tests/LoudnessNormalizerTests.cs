using Resona.Services;

namespace Resona.Tests;

public sealed class LoudnessNormalizerTests
{
    [Fact]
    public void Missing_loudness_keeps_original_level()
    {
        Assert.Equal(1f, LoudnessNormalizer.CalculateGain(null, null));
    }

    [Fact]
    public void Loud_track_is_reduced_toward_target_lufs()
    {
        Assert.Equal(0.501f, LoudnessNormalizer.CalculateGain(-8, 5), precision: 3);
    }

    [Fact]
    public void Quiet_track_boost_is_limited_to_six_decibels()
    {
        Assert.Equal(1.995f, LoudnessNormalizer.CalculateGain(-24, 5), precision: 3);
    }

    [Fact]
    public void Wide_dynamic_range_reduces_positive_gain()
    {
        var regularGain = LoudnessNormalizer.CalculateGain(-18, 5);
        var wideRangeGain = LoudnessNormalizer.CalculateGain(-18, 16);

        Assert.True(wideRangeGain < regularGain);
        Assert.True(wideRangeGain > 1f);
    }
}
