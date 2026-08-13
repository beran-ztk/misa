using System.Linq;
using Resona.Models;

namespace Resona.Tests;

public sealed class EmotionalCharacterCatalogTests
{
    [Fact]
    public void EveryCharacterHasAUniqueAccentColor()
    {
        Assert.Equal(5, EmotionalCharacterCatalog.All.Count);
        Assert.Equal(
            EmotionalCharacterCatalog.All.Count,
            EmotionalCharacterCatalog.All.Select(item => item.AccentColor).Distinct().Count());
        Assert.All(EmotionalCharacterCatalog.All, item =>
        {
            Assert.Matches("^#[0-9A-Fa-f]{6}$", item.AccentColor);
            Assert.Equal(item.AccentColor, EmotionalCharacterCatalog.Color(item.Adjectives));
        });
    }
}
