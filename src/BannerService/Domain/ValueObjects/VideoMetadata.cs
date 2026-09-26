namespace BannerService.Domain.ValueObjects;

public class VideoMetadata
{
    public TimeSpan Duration { get; set; }
    public string Resolution { get; set; } = string.Empty;  // "1920x1080"
    public decimal FrameRate { get; set; }
    public string Codec { get; set; } = string.Empty;  // "h264", "h265", "vp9"
    public long Bitrate { get; set; }  // bits per second
    public string? AudioCodec { get; set; }  // "aac", "mp3", "opus"
    public DateTime ExtractedAt { get; set; }

    public VideoMetadata() { }

    public VideoMetadata(
        TimeSpan duration,
        string resolution,
        decimal frameRate,
        string codec,
        long bitrate,
        string? audioCodec = null)
    {
        if (duration == TimeSpan.Zero || duration.TotalMilliseconds <= 0)
            throw new ArgumentException("Duration must be greater than 0");
        if (string.IsNullOrWhiteSpace(resolution))
            throw new ArgumentException("Resolution required");
        if (frameRate <= 0)
            throw new ArgumentException("FrameRate must be greater than 0");
        if (string.IsNullOrWhiteSpace(codec))
            throw new ArgumentException("Codec required");
        if (bitrate <= 0)
            throw new ArgumentException("Bitrate must be greater than 0");

        Duration = duration;
        Resolution = resolution;
        FrameRate = frameRate;
        Codec = codec;
        Bitrate = bitrate;
        AudioCodec = audioCodec;
        ExtractedAt = DateTime.UtcNow;
    }
}
