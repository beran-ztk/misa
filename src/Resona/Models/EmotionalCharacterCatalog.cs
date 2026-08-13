using System;
using System.Collections.Generic;
using System.Linq;

namespace Resona.Models;

public sealed record EmotionalCharacterDefinition(string Name, string Adjectives, string AccentColor)
{
    public string DisplayText => $"{Name} · {Adjectives}";
}

public static class EmotionalCharacterCatalog
{
    public static IReadOnlyList<EmotionalCharacterDefinition> All { get; } =
    [
        new("Empowered", "passionate, rousing, confident, boisterous, rowdy", "#E6B85C"),
        new("Joyful", "rollicking, cheerful, fun, sweet, amiable/good natured", "#75D49A"),
        new("Reflective", "literate, poignant, wistful, bittersweet, autumnal, brooding", "#79A9E8"),
        new("Playful", "humorous, silly, campy, quirky, whimsical, witty, wry", "#C38BE2"),
        new("Intense", "aggressive, fiery, tense/anxious, intense, volatile, visceral", "#E87878")
    ];

    public static EmotionalCharacterDefinition? Find(string signalKey) => All.FirstOrDefault(item =>
        string.Equals(item.Adjectives, signalKey, StringComparison.OrdinalIgnoreCase));

    public static string Display(string signalKey) => Find(signalKey)?.DisplayText ?? signalKey;
    public static string Name(string signalKey) => Find(signalKey)?.Name ?? signalKey;
    public static string Color(string signalKey) => Find(signalKey)?.AccentColor ?? "#D8D3C4";
}
