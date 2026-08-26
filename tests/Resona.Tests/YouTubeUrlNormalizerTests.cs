using Resona.Services;

namespace Resona.Tests;

public sealed class YouTubeUrlNormalizerTests
{
    [Fact]
    public void Regular_watch_url_is_a_single_track()
    {
        var source = YouTubeUrlNormalizer.ParseImportSource(
            "https://www.youtube.com/watch?v=jyeflTZ_dhg");

        Assert.Equal(
            new YouTubeImportSource(
                "https://www.youtube.com/watch?v=jyeflTZ_dhg",
                YouTubeImportSourceKind.SingleTrack),
            source);
    }

    [Fact]
    public void Watch_url_inside_a_radio_mix_is_reduced_to_the_single_track()
    {
        var source = YouTubeUrlNormalizer.ParseImportSource(
            "https://www.youtube.com/watch?v=jyeflTZ_dhg&list=RDjyeflTZ_dhg&start_radio=1&rv=jyeflTZ_dhg");

        Assert.Equal(
            new YouTubeImportSource(
                "https://www.youtube.com/watch?v=jyeflTZ_dhg",
                YouTubeImportSourceKind.SingleTrack),
            source);
    }

    [Fact]
    public void Watch_url_inside_a_regular_playlist_is_still_a_single_track()
    {
        var source = YouTubeUrlNormalizer.ParseImportSource(
            "https://www.youtube.com/watch?v=jyeflTZ_dhg&list=PL123456789");

        Assert.Equal(YouTubeImportSourceKind.SingleTrack, source?.Kind);
        Assert.Equal("https://www.youtube.com/watch?v=jyeflTZ_dhg", source?.Url);
    }

    [Fact]
    public void Explicit_playlist_url_is_a_playlist()
    {
        var source = YouTubeUrlNormalizer.ParseImportSource(
            "https://music.youtube.com/playlist?list=PL123456789&feature=shared");

        Assert.Equal(
            new YouTubeImportSource(
                "https://www.youtube.com/playlist?list=PL123456789",
                YouTubeImportSourceKind.Playlist),
            source);
    }

    [Fact]
    public void Explicit_generated_radio_playlist_is_not_supported()
    {
        var source = YouTubeUrlNormalizer.ParseImportSource(
            "https://www.youtube.com/playlist?list=RDjyeflTZ_dhg");

        Assert.Null(source);
    }

    [Theory]
    [InlineData("https://www.youtube.com/@deadmau5")]
    [InlineData("https://example.com/watch?v=jyeflTZ_dhg")]
    [InlineData("not a url")]
    public void Other_sources_are_not_supported(string url) =>
        Assert.Null(YouTubeUrlNormalizer.ParseImportSource(url));
}
