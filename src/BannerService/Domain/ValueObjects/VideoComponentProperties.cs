namespace BannerService.Domain.ValueObjects;

public class VideoComponentProperties
{
    public string VideoReference { get; set; } = string.Empty;
    public bool Autoplay { get; set; } = false;
    public bool Loop { get; set; } = false;
    public bool Muted { get; set; } = true;
    public float Volume { get; set; } = 1.0f;
    public int StartTime { get; set; } = 0;
    public int Duration { get; set; } = int.MaxValue;

    public VideoComponentProperties() { }

    public VideoComponentProperties(
        string videoReference,
        bool autoplay,
        bool loop,
        bool muted,
        float volume,
        int startTime,
        int duration)
    {
        Validate(videoReference, volume, duration);

        VideoReference = videoReference;
        Autoplay = autoplay;
        Loop = loop;
        Muted = muted;
        Volume = volume;
        StartTime = startTime;
        Duration = duration;
    }

    private static void Validate(string videoReference, float volume, int duration)
    {
        if (string.IsNullOrWhiteSpace(videoReference))
            throw new ArgumentException("VideoReference cannot be empty");

        if (!Uri.TryCreate(videoReference, UriKind.Absolute, out _))
            throw new ArgumentException("VideoReference must be a valid URL");

        if (volume < 0 || volume > 1)
            throw new ArgumentException("Volume must be 0-1");

        if (duration <= 0)
            throw new ArgumentException("Duration must be positive");
    }
}
