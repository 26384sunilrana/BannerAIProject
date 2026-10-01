using System.Text.Json;
using BannerService.Domain.Services;
using BannerService.Domain.ValueObjects;
using Xunit;

namespace BannerService.Domain.Tests.ValueObjects;

public class VideoPlaylistTests
{
    private static VideoPlaylistItem Item(string name, double? seconds = null) =>
        new() { MediaUrl = $"https://media.example.com/{name}.mp4", DurationSeconds = seconds };

    private static VideoPlaylist Playlist(VideoRotationMode mode, int? seconds = null, params VideoPlaylistItem[] items) =>
        new() { Playlist = items.ToList(), RotationMode = mode, SecondsPerVideo = seconds };

    [Fact]
    public void PlayFull_EachVideoRunsToItsOwnEnd()
    {
        var plan = Playlist(VideoRotationMode.PlayFull, null, Item("a", 30), Item("b", 12.5)).BuildPlan();

        Assert.Equal(new double?[] { 30, 12.5 }, plan.Steps.Select(s => s.PlaySeconds));
        Assert.Equal(42.5, plan.CycleSeconds);
    }

    [Fact]
    public void PlayFull_UnknownLength_MeansUntilItEnds()
    {
        var plan = Playlist(VideoRotationMode.PlayFull, null, Item("a")).BuildPlan();

        Assert.Null(plan.Steps[0].PlaySeconds);
        Assert.Null(plan.CycleSeconds);
        Assert.True(plan.Steps[0].AdvanceWhenEnded);
    }

    [Fact]
    public void FixedSeconds_LongVideoIsCutAtConfiguredTime()
    {
        var plan = Playlist(VideoRotationMode.FixedSeconds, 10, Item("long", 60)).BuildPlan();

        Assert.Equal(10, plan.Steps[0].PlaySeconds);
    }

    [Fact]
    public void FixedSeconds_ShortVideoRotatesWhenItEnds()
    {
        var plan = Playlist(VideoRotationMode.FixedSeconds, 10, Item("short", 4), Item("long", 60)).BuildPlan();

        Assert.Equal(4, plan.Steps[0].PlaySeconds);
        Assert.Equal(10, plan.Steps[1].PlaySeconds);
        Assert.Equal(14, plan.CycleSeconds);
    }

    [Fact]
    public void FixedSeconds_UnknownLength_UsesConfiguredTimeAndStillAdvancesOnEnd()
    {
        var step = Playlist(VideoRotationMode.FixedSeconds, 8, Item("unknown")).BuildPlan().Steps[0];

        Assert.Equal(8, step.PlaySeconds);
        Assert.True(step.AdvanceWhenEnded);
    }

    [Fact]
    public void Muted_ForcesVolumeToZero_UnmutedKeepsConfiguredVolume()
    {
        var muted = Playlist(VideoRotationMode.PlayFull, null, Item("a", 5));
        muted.Muted = true;
        muted.Volume = 0.8;
        var loud = Playlist(VideoRotationMode.PlayFull, null, Item("a", 5));
        loud.Muted = false;
        loud.Volume = 0.8;

        Assert.Equal(0, muted.BuildPlan().Volume);
        Assert.Equal(0.8, loud.BuildPlan().Volume);
    }

    [Fact]
    public void Validate_FixedSecondsWithoutSeconds_Throws()
    {
        Assert.Throws<ArgumentException>(() => Playlist(VideoRotationMode.FixedSeconds, null, Item("a")).Validate());
        Assert.Throws<ArgumentException>(() => Playlist(VideoRotationMode.FixedSeconds, 0, Item("a")).Validate());
        Assert.Throws<ArgumentException>(() => Playlist(VideoRotationMode.FixedSeconds, 4000, Item("a")).Validate());
    }

    [Fact]
    public void Validate_RejectsEmptyTooLongBadUrlAndBadVolume()
    {
        Assert.Throws<ArgumentException>(() => Playlist(VideoRotationMode.PlayFull).Validate());
        Assert.Throws<ArgumentException>(() => Playlist(VideoRotationMode.PlayFull, null,
            Enumerable.Range(0, 21).Select(i => Item($"v{i}")).ToArray()).Validate());
        Assert.Throws<ArgumentException>(() => Playlist(VideoRotationMode.PlayFull, null,
            new VideoPlaylistItem { MediaUrl = "not a url" }).Validate());
        var loud = Playlist(VideoRotationMode.PlayFull, null, Item("a"));
        loud.Volume = 1.5;
        Assert.Throws<ArgumentException>(() => loud.Validate());
    }

    [Fact]
    public void FromProperties_ReadsClientJson_AndIgnoresComponentsWithoutPlaylist()
    {
        var json = """
        {
          "playlist": [ { "mediaUrl": "https://media.example.com/a.mp4", "durationSeconds": 20 },
                        { "mediaUrl": "https://media.example.com/b.mp4" } ],
          "rotationMode": "fixedSeconds",
          "secondsPerVideo": 7,
          "muted": false,
          "volume": 0.5
        }
        """;
        var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;

        var playlist = VideoPlaylist.FromProperties(properties);

        Assert.NotNull(playlist);
        Assert.Equal(VideoRotationMode.FixedSeconds, playlist!.RotationMode);
        Assert.Equal(7, playlist.SecondsPerVideo);
        Assert.False(playlist.Muted);
        Assert.Equal(new double?[] { 7, 7 }, playlist.BuildPlan().Steps.Select(s => s.PlaySeconds));

        Assert.Null(VideoPlaylist.FromProperties(new Dictionary<string, object> { ["mediaUrl"] = "https://x.example.com/v.mp4" }));
    }

    [Fact]
    public void ComponentValidation_RejectsInvalidPlaylist_ButAllowsLegacySingleVideo()
    {
        var service = new ComponentValidationService();
        var bad = JsonSerializer.Deserialize<Dictionary<string, object>>(
            """{ "playlist": [ { "mediaUrl": "https://m.example.com/a.mp4" } ], "rotationMode": "fixedSeconds" }""")!;
        var legacy = new Dictionary<string, object> { ["mediaUrl"] = "https://m.example.com/a.mp4", ["muted"] = true };

        Assert.Throws<ArgumentException>(() => service.ValidateVideoPlaylist(bad));
        service.ValidateVideoPlaylist(legacy);
        service.ValidateVideoPlaylist(null);
    }
}
