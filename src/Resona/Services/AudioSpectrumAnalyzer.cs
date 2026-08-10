using System;

namespace Resona.Services;

/// <summary>
/// Collects mono PCM samples and publishes a logarithmic 20 Hz - 20 kHz spectrum.
/// The implementation is shared by the live Windows path and Linux's FFmpeg timeline.
/// </summary>
internal sealed class AudioSpectrumAnalyzer
{
    public const int BandCount = 48;
    private const int FftSize = 4096;
    private const double MinimumFrequency = 20;
    private const double MaximumFrequency = 20_000;
    private const double MinimumDecibels = -72;
    private const double MaximumDecibels = -8;

    private readonly int _sampleRate;
    private readonly double[] _real = new double[FftSize];
    private readonly double[] _imaginary = new double[FftSize];
    private readonly double[] _window = new double[FftSize];
    private readonly double _amplitudeScale;
    private int _sampleIndex;

    public AudioSpectrumAnalyzer(int sampleRate)
    {
        _sampleRate = Math.Max(1, sampleRate);
        double windowSum = 0;
        for (var i = 0; i < FftSize; i++)
        {
            _window[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (FftSize - 1));
            windowSum += _window[i];
        }

        _amplitudeScale = 2d / windowSum;
    }

    public float[] LatestSpectrum { get; private set; } = new float[BandCount];

    public bool AddSample(double sample)
    {
        if (!double.IsFinite(sample))
            sample = 0;

        _real[_sampleIndex] = sample * _window[_sampleIndex];
        _imaginary[_sampleIndex] = 0;
        _sampleIndex++;
        if (_sampleIndex < FftSize)
            return false;

        Transform(_real, _imaginary);
        LatestSpectrum = CreateBands();
        _sampleIndex = 0;
        return true;
    }

    private float[] CreateBands()
    {
        var bands = new float[BandCount];
        var highestFrequency = Math.Min(MaximumFrequency, _sampleRate / 2d);
        var logRange = Math.Log(MaximumFrequency / MinimumFrequency);

        for (var band = 0; band < BandCount; band++)
        {
            var lower = MinimumFrequency * Math.Exp(logRange * band / BandCount);
            var upper = MinimumFrequency * Math.Exp(logRange * (band + 1) / BandCount);
            if (lower >= highestFrequency)
                continue;

            upper = Math.Min(upper, highestFrequency);
            var firstBin = Math.Clamp((int)Math.Floor(lower * FftSize / _sampleRate), 1, FftSize / 2);
            var lastBin = Math.Clamp((int)Math.Ceiling(upper * FftSize / _sampleRate), firstBin, FftSize / 2);
            double peakMagnitude = 0;
            for (var bin = firstBin; bin <= lastBin; bin++)
            {
                var magnitude = Math.Sqrt(
                    _real[bin] * _real[bin] + _imaginary[bin] * _imaginary[bin]) * _amplitudeScale;
                peakMagnitude = Math.Max(peakMagnitude, magnitude);
            }

            var decibels = 20 * Math.Log10(Math.Max(peakMagnitude, 1e-9));
            var normalized = Math.Clamp(
                (decibels - MinimumDecibels) / (MaximumDecibels - MinimumDecibels), 0, 1);
            bands[band] = (float)Math.Pow(normalized, 0.78);
        }

        return bands;
    }

    private static void Transform(double[] real, double[] imaginary)
    {
        var length = real.Length;
        for (int i = 1, j = 0; i < length; i++)
        {
            var bit = length >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;
            if (i >= j)
                continue;
            (real[i], real[j]) = (real[j], real[i]);
            (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
        }

        for (var size = 2; size <= length; size <<= 1)
        {
            var angle = -2 * Math.PI / size;
            var stepReal = Math.Cos(angle);
            var stepImaginary = Math.Sin(angle);
            for (var start = 0; start < length; start += size)
            {
                double phaseReal = 1;
                double phaseImaginary = 0;
                var half = size >> 1;
                for (var offset = 0; offset < half; offset++)
                {
                    var even = start + offset;
                    var odd = even + half;
                    var oddReal = real[odd] * phaseReal - imaginary[odd] * phaseImaginary;
                    var oddImaginary = real[odd] * phaseImaginary + imaginary[odd] * phaseReal;
                    real[odd] = real[even] - oddReal;
                    imaginary[odd] = imaginary[even] - oddImaginary;
                    real[even] += oddReal;
                    imaginary[even] += oddImaginary;

                    var nextPhaseReal = phaseReal * stepReal - phaseImaginary * stepImaginary;
                    phaseImaginary = phaseReal * stepImaginary + phaseImaginary * stepReal;
                    phaseReal = nextPhaseReal;
                }
            }
        }
    }
}
