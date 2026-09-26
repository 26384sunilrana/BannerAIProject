namespace BannerService.Domain.Services;

using ValueObjects;
using System.Diagnostics;
using System.Text.Json;

public class MetadataExtractionService
{
    private readonly string _ffprobePath;
    private readonly ILogger<MetadataExtractionService> _logger;

    public MetadataExtractionService(IConfiguration configuration, ILogger<MetadataExtractionService> logger)
    {
        _ffprobePath = configuration["MediaService:FFprobePath"] ?? "ffprobe";
        _logger = logger;
    }

    public async Task<VideoMetadata?> ExtractVideoMetadataAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        try
        {
            var process = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(process))
            {
                if (proc == null)
                    throw new InvalidOperationException("Failed to start FFprobe process");

                var output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                if (proc.ExitCode != 0)
                {
                    _logger.LogWarning($"FFprobe failed with exit code {proc.ExitCode}");
                    return null;
                }

                return ParseFFprobeOutput(output);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting video metadata");
            return null;
        }
    }

    private VideoMetadata? ParseFFprobeOutput(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("format", out var format))
                return null;

            // Extract duration
            if (!format.TryGetProperty("duration", out var durationElem) || !double.TryParse(durationElem.GetString(), out var durationSecs))
                return null;

            var duration = TimeSpan.FromSeconds(durationSecs);

            // Get streams
            if (!root.TryGetProperty("streams", out var streams))
                return null;

            var videoStream = streams.EnumerateArray()
                .FirstOrDefault(s => s.TryGetProperty("codec_type", out var type) && type.GetString() == "video");

            if (videoStream.ValueKind == JsonValueKind.Undefined)
                return null;

            // Extract video properties
            var resolution = GetResolution(videoStream);
            var frameRate = GetFrameRate(videoStream);
            var codec = GetCodec(videoStream);
            var bitrate = GetBitrate(videoStream, format);

            var audioStream = streams.EnumerateArray()
                .FirstOrDefault(s => s.TryGetProperty("codec_type", out var type) && type.GetString() == "audio");

            var audioCodec = audioStream.ValueKind != JsonValueKind.Undefined ? GetCodec(audioStream) : null;

            return new VideoMetadata(duration, resolution, frameRate, codec, bitrate, audioCodec);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing FFprobe output");
            return null;
        }
    }

    private string GetResolution(JsonElement stream)
    {
        if (stream.TryGetProperty("width", out var width) &&
            stream.TryGetProperty("height", out var height) &&
            width.TryGetInt32(out var w) &&
            height.TryGetInt32(out var h))
        {
            return $"{w}x{h}";
        }
        return "unknown";
    }

    private decimal GetFrameRate(JsonElement stream)
    {
        if (stream.TryGetProperty("r_frame_rate", out var frameRateStr))
        {
            var parts = frameRateStr.GetString()?.Split('/');
            if (parts?.Length == 2 &&
                decimal.TryParse(parts[0], out var num) &&
                decimal.TryParse(parts[1], out var den) &&
                den != 0)
            {
                return num / den;
            }
        }
        return 30m;  // Default to 30fps
    }

    private string GetCodec(JsonElement stream)
    {
        if (stream.TryGetProperty("codec_name", out var codec))
        {
            return codec.GetString() ?? "unknown";
        }
        return "unknown";
    }

    private long GetBitrate(JsonElement videoStream, JsonElement format)
    {
        if (videoStream.TryGetProperty("bit_rate", out var streamBitrate) &&
            long.TryParse(streamBitrate.GetString(), out var bitrate))
        {
            return bitrate;
        }

        if (format.TryGetProperty("bit_rate", out var formatBitrate) &&
            long.TryParse(formatBitrate.GetString(), out var fbr))
        {
            return fbr;
        }

        return 0;
    }
}
