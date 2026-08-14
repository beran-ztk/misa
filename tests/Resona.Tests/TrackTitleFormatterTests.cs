using System.Collections.Generic;
using Resona.Models;
using Resona.Services;

namespace Resona.Tests;

public sealed class TrackTitleFormatterTests
{
    [Fact]
    public void Empty_optional_components_preserve_the_existing_title()
    {
        Assert.Equal("Rightfully", TrackTitleFormatter.Format(null, "Rightfully", null, null));
    }

    [Fact]
    public void Components_are_formatted_with_remix_parentheses_and_edit_separators()
    {
        Assert.Equal(
            "Mili — Rightfully (Zenkaso Remix) · Nightcore · Sped Up",
            TrackTitleFormatter.Format(" Mili ", " Rightfully ", " Zenkaso Remix ", "Nightcore, Sped Up"));
    }

    [Fact]
    public void Empty_and_duplicate_edit_entries_are_removed()
    {
        Assert.Equal(
            "Song · Slowed · Reverb",
            TrackTitleFormatter.Format(null, "Song", null, " Slowed, , Reverb, Slowed "));
    }

    [Fact]
    public void Search_matches_artist_remix_and_edits_through_the_display_title()
    {
        var track = new MusicTrack(
            1, string.Empty, "Rightfully", "1.mp3", null, "2026-01-01T00:00:00Z", null,
            false, null, null, null, "2026-01-01T00:00:00Z",
            Artist: "Mili", Remix: "Zenkaso Remix", Edits: "Nightcore, Sped Up");

        List<MusicTrack> Search(string term) => TrackFilter.Apply(
            [track],
            new Dictionary<int, List<int>>(),
            new Dictionary<int, List<int>>(),
            new Dictionary<int, List<int>>(),
            new Dictionary<int, Dictionary<string, double>>(),
            new HashSet<int>(),
            [],
            term);

        Assert.Single(Search("Mili"));
        Assert.Single(Search("Zenkaso"));
        Assert.Single(Search("Sped Up"));
    }
}
