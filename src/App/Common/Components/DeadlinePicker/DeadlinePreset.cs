using System;

namespace Misa.Ui.Avalonia.Common.Components.DeadlinePicker;

public sealed class DeadlinePreset
{
    private readonly Func<DateTimeOffset> _resolver;

    public string Label { get; }

    public string DisplayValue => FormatDisplayValue(Resolve());

    public DeadlinePreset(string label, Func<DateTimeOffset> resolver)
    {
        Label = label;
        _resolver = resolver;
    }

    public DateTimeOffset Resolve() => _resolver();

    private static string FormatDisplayValue(DateTimeOffset value)
    {
        var local = value.ToLocalTime();
        return local.ToString("dd.MM.yyyy HH:mm");
    }
}