using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Music.Core;

namespace Music.Services;

public static class FilterPresetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static List<PortableFilterPreset> Load()
    {
        try
        {
            if (!File.Exists(Values.FilterPresetsPath))
                return [];

            using var stream = File.OpenRead(Values.FilterPresetsPath);
            return JsonSerializer.Deserialize<List<PortableFilterPreset>>(stream, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void Save(List<PortableFilterPreset> presets)
    {
        var directory = Path.GetDirectoryName(Values.FilterPresetsPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var sortedPresets = presets
            .Where(preset => !string.IsNullOrWhiteSpace(preset.Name))
            .OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var stream = File.Create(Values.FilterPresetsPath);
        JsonSerializer.Serialize(stream, sortedPresets, JsonOptions);
    }
}
