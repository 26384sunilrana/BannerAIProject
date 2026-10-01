namespace BannerService.Domain.ValueObjects;

using System.Text.Json;
using System.Text.Json.Serialization;

public enum VideoRotationMode
{
    /// <summary>Play each video to its end, then move to the next one.</summary>
    PlayFull = 1,

    /// <summary>Play each video for a fixed number of seconds, then move to the next one.</summary>
    FixedSeconds = 2
}

public class VideoPlaylistItem
{
    public string MediaUrl { get; set; } = string.Empty;

    /// <summary>Length of the video in seconds when known (from the media metadata).</summary>
    public double? DurationSeconds { get; set; }
}

/// <summary>One step of the playback plan handed to the client.</summary>
public class VideoPlayStep
{
    public int Index { get; set; }
    public string MediaUrl { get; set; } = string.Empty;

    /// <summary>Seconds to show this video; null means "until it ends" (length unknown).</summary>
    public double? PlaySeconds { get; set; }

    /// <summary>The client always moves on when a video ends, even before PlaySeconds (a short video).</summary>
    public bool AdvanceWhenEnded { get; set; } = true;
}

public class VideoPlaybackPlan
{
    public bool Muted { get; set; }
    public double Volume { get; set; }
    public bool Loop { get; set; }
    public List<VideoPlayStep> Steps { get; set; } = new();

    /// <summary>Total length of one pass through the playlist; null when any step length is unknown.</summary>
    public double? CycleSeconds => Steps.Any(s => s.PlaySeconds == null) ? null : Steps.Sum(s => s.PlaySeconds!.Value);
}

/// <summary>A list of videos in one video component, with rotation and sound settings.</summary>
public class VideoPlaylist
{
    public const int MaxItems = 20;
    public const int MaxSecondsPerVideo = 3600;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public List<VideoPlaylistItem> Playlist { get; set; } = new();
    public VideoRotationMode RotationMode { get; set; } = VideoRotationMode.PlayFull;
    public int? SecondsPerVideo { get; set; }
    public bool Muted { get; set; } = true;
    public double Volume { get; set; } = 1.0;
    public bool Loop { get; set; } = true;

    /// <summary>Reads a playlist from component properties; returns null when the component has none.</summary>
    public static VideoPlaylist? FromProperties(IDictionary<string, object>? properties)
    {
        if (properties == null)
            return null;

        var hasPlaylist = properties.Keys.Any(k => string.Equals(k, "playlist", StringComparison.OrdinalIgnoreCase));
        if (!hasPlaylist)
            return null;

        try
        {
            return JsonSerializer.Deserialize<VideoPlaylist>(JsonSerializer.Serialize(properties), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid video playlist: {ex.Message}");
        }
    }

    public void Validate()
    {
        if (Playlist.Count == 0)
            throw new ArgumentException("A video playlist needs at least one video");
        if (Playlist.Count > MaxItems)
            throw new ArgumentException($"A video playlist can hold at most {MaxItems} videos");

        foreach (var item in Playlist)
        {
            if (!Uri.TryCreate(item.MediaUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Every playlist video needs a valid mediaUrl");
            if (item.DurationSeconds is <= 0)
                throw new ArgumentException("A video's durationSeconds must be positive");
        }

        if (RotationMode == VideoRotationMode.FixedSeconds)
        {
            if (SecondsPerVideo is null or < 1 or > MaxSecondsPerVideo)
                throw new ArgumentException($"secondsPerVideo must be 1-{MaxSecondsPerVideo} when rotationMode is fixedSeconds");
        }
        else if (!Enum.IsDefined(RotationMode))
        {
            throw new ArgumentException("rotationMode must be playFull or fixedSeconds");
        }

        if (Volume < 0 || Volume > 1)
            throw new ArgumentException("Volume must be 0-1");
    }

    public VideoPlaybackPlan BuildPlan()
    {
        Validate();

        return new VideoPlaybackPlan
        {
            Muted = Muted,
            Volume = Muted ? 0 : Volume,
            Loop = Loop,
            Steps = Playlist.Select((item, index) => new VideoPlayStep
            {
                Index = index,
                MediaUrl = item.MediaUrl,
                PlaySeconds = PlaySecondsFor(item)
            }).ToList()
        };
    }

    // A video shorter than the configured time rotates when it ends, so the shorter value wins.
    private double? PlaySecondsFor(VideoPlaylistItem item)
    {
        if (RotationMode == VideoRotationMode.PlayFull)
            return item.DurationSeconds;

        var configured = (double)SecondsPerVideo!.Value;
        return item.DurationSeconds is { } length ? Math.Min(configured, length) : configured;
    }
}
