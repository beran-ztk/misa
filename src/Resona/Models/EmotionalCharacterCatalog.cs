using System;
using System.Collections.Generic;
using System.Linq;

namespace Resona.Models;

public sealed record EmotionalCharacterDefinition(string Name, string Adjectives)
{
    public string DisplayText => $"{Name} · {Adjectives}";
}

public static class EmotionalCharacterCatalog
{
    public static IReadOnlyList<EmotionalCharacterDefinition> All { get; } =
    [
        new("Empowered", "passionate, rousing, confident, boisterous, rowdy"),
        new("Joyful", "rollicking, cheerful, fun, sweet, amiable/good natured"),
        new("Reflective", "literate, poignant, wistful, bittersweet, autumnal, brooding"),
        new("Playful", "humorous, silly, campy, quirky, whimsical, witty, wry"),
        new("Intense", "aggressive, fiery, tense/anxious, intense, volatile, visceral")
    ];

    public static EmotionalCharacterDefinition? Find(string signalKey) => All.FirstOrDefault(item =>
        string.Equals(item.Adjectives, signalKey, StringComparison.OrdinalIgnoreCase));

    public static string Display(string signalKey) => Find(signalKey)?.DisplayText ?? signalKey;
    public static string Name(string signalKey) => Find(signalKey)?.Name ?? signalKey;
}
