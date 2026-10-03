using System.Text.Json;
using BannerService.Domain.Services;
using BannerService.Domain.ValueObjects;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class ComponentSettingsValidatorTests
{
    /// <summary>Properties the way they arrive from JSON: values are JsonElements.</summary>
    private static Dictionary<string, object> Props(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;

    // ----- effects

    [Fact]
    public void NoEffect_IsFine()
    {
        ComponentSettingsValidator.ValidateEffect(Props("{\"content\":\"hi\"}"));
        ComponentSettingsValidator.ValidateEffect(null);
        ComponentSettingsValidator.ValidateEffect(Props("{\"effect\":null}"));
    }

    [Theory]
    [InlineData("none")]
    [InlineData("fadeIn")]
    [InlineData("slideLeft")]
    [InlineData("slideRight")]
    [InlineData("slideUp")]
    [InlineData("zoomIn")]
    [InlineData("pulse")]
    [InlineData("float")]
    public void EveryKnownEffect_IsAccepted(string kind)
    {
        ComponentSettingsValidator.ValidateEffect(Props($"{{\"effect\":{{\"kind\":\"{kind}\",\"durationMs\":800,\"delayMs\":0}}}}"));
    }

    [Theory]
    [InlineData("{\"effect\":{\"kind\":\"explode\",\"durationMs\":800,\"delayMs\":0}}")]
    [InlineData("{\"effect\":{\"durationMs\":800,\"delayMs\":0}}")]
    [InlineData("{\"effect\":{\"kind\":\"fadeIn\",\"durationMs\":50,\"delayMs\":0}}")]
    [InlineData("{\"effect\":{\"kind\":\"fadeIn\",\"durationMs\":20000,\"delayMs\":0}}")]
    [InlineData("{\"effect\":{\"kind\":\"fadeIn\",\"durationMs\":800,\"delayMs\":-1}}")]
    [InlineData("{\"effect\":{\"kind\":\"fadeIn\",\"durationMs\":800,\"delayMs\":99999}}")]
    [InlineData("{\"effect\":{\"kind\":\"fadeIn\",\"durationMs\":\"fast\",\"delayMs\":0}}")]
    [InlineData("{\"effect\":{\"kind\":\"fadeIn\",\"durationMs\":800}}")]
    [InlineData("{\"effect\":\"fadeIn\"}")]
    public void BadEffects_AreRefused(string json)
    {
        Assert.Throws<ArgumentException>(() => ComponentSettingsValidator.ValidateEffect(Props(json)));
    }

    // ----- rotating pictures

    private static string Slides(int count) =>
        "[" + string.Join(",", Enumerable.Range(0, count).Select(i => $"{{\"mediaFileId\":\"file-{i}\",\"alt\":\"Picture {i}\"}}")) + "]";

    [Fact]
    public void ARotatingPictureList_IsAccepted()
    {
        ComponentSettingsValidator.ValidateImageSlides(Props(
            $"{{\"slides\":{Slides(3)},\"slideIntervalSeconds\":5,\"slideTransition\":\"slideLeft\",\"slideTransitionMs\":600}}"));
    }

    [Fact]
    public void AnImageWithoutSlides_IsFine()
    {
        ComponentSettingsValidator.ValidateImageSlides(Props("{\"mediaFileId\":\"abc\"}"));
        ComponentSettingsValidator.ValidateImageSlides(null);
    }

    [Fact]
    public void TwentySlidesAreAllowed_TwentyOneAreNot()
    {
        ComponentSettingsValidator.ValidateImageSlides(Props($"{{\"slides\":{Slides(20)}}}"));
        Assert.Throws<ArgumentException>(() => ComponentSettingsValidator.ValidateImageSlides(Props($"{{\"slides\":{Slides(21)}}}")));
    }

    [Theory]
    [InlineData("{\"slides\":\"abc\"}")]
    [InlineData("{\"slides\":[\"abc\"]}")]
    [InlineData("{\"slides\":[{\"alt\":\"no id\"}]}")]
    [InlineData("{\"slides\":[{\"mediaFileId\":\"\"}]}")]
    [InlineData("{\"slideIntervalSeconds\":0}")]
    [InlineData("{\"slideIntervalSeconds\":61}")]
    [InlineData("{\"slideIntervalSeconds\":\"soon\"}")]
    [InlineData("{\"slideTransition\":\"spin\"}")]
    [InlineData("{\"slideTransitionMs\":100}")]
    [InlineData("{\"slideTransitionMs\":5000}")]
    public void BadSlideSettings_AreRefused(string json)
    {
        Assert.Throws<ArgumentException>(() => ComponentSettingsValidator.ValidateImageSlides(Props(json)));
    }

    [Fact]
    public void AnOverlongAltText_IsRefused()
    {
        var alt = new string('x', 201);

        Assert.Throws<ArgumentException>(() =>
            ComponentSettingsValidator.ValidateImageSlides(Props($"{{\"slides\":[{{\"mediaFileId\":\"a\",\"alt\":\"{alt}\"}}]}}")));
    }

    [Fact]
    public void BoundaryValues_AreAccepted()
    {
        ComponentSettingsValidator.ValidateImageSlides(Props("{\"slideIntervalSeconds\":1,\"slideTransitionMs\":200}"));
        ComponentSettingsValidator.ValidateImageSlides(Props("{\"slideIntervalSeconds\":60,\"slideTransitionMs\":2000}"));
        ComponentSettingsValidator.ValidateEffect(Props("{\"effect\":{\"kind\":\"pulse\",\"durationMs\":100,\"delayMs\":30000}}"));
        ComponentSettingsValidator.ValidateEffect(Props("{\"effect\":{\"kind\":\"pulse\",\"durationMs\":10000,\"delayMs\":0}}"));
    }
}

public class VideoPlaylistByFileTests
{
    private static VideoPlaylist Playlist(string json) =>
        VideoPlaylist.FromProperties(JsonSerializer.Deserialize<Dictionary<string, object>>(json))!;

    [Fact]
    public void AVideoIsIdentifiedByItsUploadedFile_NoLinkNeeded()
    {
        var playlist = Playlist("{\"playlist\":[{\"mediaFileId\":\"f1\",\"durationSeconds\":12.5},{\"mediaFileId\":\"f2\",\"durationSeconds\":30}],\"rotationMode\":\"playFull\"}");

        playlist.Validate();
        var plan = playlist.BuildPlan();

        Assert.Equal(new[] { "f1", "f2" }, plan.Steps.Select(s => s.MediaFileId));
        Assert.Equal(new double?[] { 12.5, 30 }, plan.Steps.Select(s => s.PlaySeconds));
    }

    [Fact]
    public void ALinkAloneStillWorks()
    {
        Playlist("{\"playlist\":[{\"mediaUrl\":\"https://example.com/a.mp4\"}]}").Validate();
    }

    [Fact]
    public void AVideoWithNeitherAFileNorALink_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => Playlist("{\"playlist\":[{\"durationSeconds\":5}]}").Validate());
        Assert.Throws<ArgumentException>(() => Playlist("{\"playlist\":[{\"mediaUrl\":\"not a link\"}]}").Validate());
    }

    [Fact]
    public void ShorterVideosRotateWhenTheyEnd_LongerOnesAtTheConfiguredSeconds()
    {
        var playlist = Playlist("{\"playlist\":[{\"mediaFileId\":\"short\",\"durationSeconds\":4},{\"mediaFileId\":\"long\",\"durationSeconds\":60},{\"mediaFileId\":\"unknown\"}]," +
                                "\"rotationMode\":\"fixedSeconds\",\"secondsPerVideo\":10}");

        var plan = playlist.BuildPlan();

        Assert.Equal(new double?[] { 4, 10, 10 }, plan.Steps.Select(s => s.PlaySeconds));
        Assert.Equal(24, plan.CycleSeconds);
    }
}
