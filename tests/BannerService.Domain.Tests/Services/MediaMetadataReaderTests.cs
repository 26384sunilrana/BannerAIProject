using BannerService.Domain.Services;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class MediaMetadataReaderTests
{
    private static MediaMetadata Read(byte[] bytes, string type) => MediaMetadataReader.Read(new MemoryStream(bytes), type);

    private static byte[] Be32(uint v) => new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
    private static byte[] Be16(int v) => new[] { (byte)(v >> 8), (byte)v };
    private static byte[] Join(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
    private static byte[] Ascii(string s) => System.Text.Encoding.ASCII.GetBytes(s);

    // ----- pictures

    [Fact]
    public void Png_WidthAndHeightComeFromTheHeaderChunk()
    {
        var png = Join(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, Be32(13), Ascii("IHDR"), Be32(1920), Be32(1080), new byte[20]);

        Assert.Equal(new MediaMetadata(1920, 1080, null), Read(png, "image/png"));
    }

    [Fact]
    public void Gif_WidthAndHeightAreLittleEndian()
    {
        var gif = Join(Ascii("GIF89a"), new byte[] { 0x40, 0x01, 0xF0, 0x00 }, new byte[10]);

        Assert.Equal(new MediaMetadata(320, 240, null), Read(gif, "image/gif"));
    }

    [Fact]
    public void Jpeg_SkipsOtherSegmentsToFindTheFrameHeader()
    {
        var app0 = Join(new byte[] { 0xFF, 0xE0 }, Be16(16), new byte[14]);
        var comment = Join(new byte[] { 0xFF, 0xFE }, Be16(6), new byte[4]);
        var frame = Join(new byte[] { 0xFF, 0xC0 }, Be16(17), new byte[] { 8 }, Be16(600), Be16(800), new byte[10]);
        var jpeg = Join(new byte[] { 0xFF, 0xD8 }, app0, comment, frame);

        Assert.Equal(new MediaMetadata(800, 600, null), Read(jpeg, "image/jpeg"));
    }

    [Fact]
    public void Jpeg_WithoutAFrameHeader_IsEmptyNotAnError()
    {
        var jpeg = Join(new byte[] { 0xFF, 0xD8 }, new byte[] { 0xFF, 0xE0 }, Be16(4), new byte[2]);

        Assert.Equal(MediaMetadata.Empty, Read(jpeg, "image/jpeg"));
    }

    [Fact]
    public void WebpExtended_UsesTwentyFourBitSizesMinusOne()
    {
        var webp = Join(Ascii("RIFF"), new byte[4], Ascii("WEBP"), Ascii("VP8X"), new byte[] { 10, 0, 0, 0 }, new byte[] { 0, 0, 0, 0 },
            new byte[] { 0x7F, 0x07, 0x00 }, new byte[] { 0x37, 0x04, 0x00 });

        Assert.Equal(new MediaMetadata(1920, 1080, null), Read(webp, "image/webp"));
    }

    [Fact]
    public void WebpLossless_PacksSizesInto14Bits()
    {
        uint bits = (uint)(639 | (479 << 14));
        var webp = Join(Ascii("RIFF"), new byte[4], Ascii("WEBP"), Ascii("VP8L"), new byte[4], new byte[] { 0x2F },
            new[] { (byte)bits, (byte)(bits >> 8), (byte)(bits >> 16), (byte)(bits >> 24) }, new byte[6]);

        Assert.Equal(new MediaMetadata(640, 480, null), Read(webp, "image/webp"));
    }

    [Fact]
    public void WebpLossy_ReadsTheFrameSize()
    {
        var webp = Join(Ascii("RIFF"), new byte[4], Ascii("WEBP"), Ascii("VP8 "), new byte[4], new byte[3], new byte[] { 0x9D, 0x01, 0x2A },
            new byte[] { 0x20, 0x03, 0x58, 0x02 }, new byte[4]);

        Assert.Equal(new MediaMetadata(800, 600, null), Read(webp, "image/webp"));
    }

    // ----- MP4

    private static byte[] Box(string type, params byte[][] payload)
    {
        var body = Join(payload);
        return Join(Be32((uint)(body.Length + 8)), Ascii(type), body);
    }

    private static byte[] Mvhd(uint timescale, uint duration) =>
        Box("mvhd", new byte[4], Be32(0), Be32(0), Be32(timescale), Be32(duration), new byte[80]);

    private static byte[] Tkhd(uint width, uint height) =>
        Box("tkhd", new byte[] { 0, 0, 0, 7 }, new byte[72], Be32(width << 16), Be32(height << 16));

    [Fact]
    public void Mp4_LengthAndSizeComeFromTheMovieAndTrackHeaders()
    {
        var audioTrack = Box("trak", Tkhd(0, 0));
        var videoTrack = Box("trak", Tkhd(1280, 720));
        var mp4 = Join(Box("ftyp", Ascii("isom"), new byte[8]), Box("moov", Mvhd(1000, 12_500), audioTrack, videoTrack), Box("mdat", new byte[64]));

        var info = Read(mp4, "video/mp4");

        Assert.Equal(1280, info.Width);
        Assert.Equal(720, info.Height);
        Assert.Equal(12.5, info.DurationSeconds);
    }

    [Fact]
    public void Mp4_WithTheMovieBoxAfterTheMediaData_StillWorks()
    {
        var mp4 = Join(Box("ftyp", Ascii("isom"), new byte[8]), Box("mdat", new byte[5000]), Box("moov", Mvhd(600, 3600), Box("trak", Tkhd(640, 360))));

        var info = Read(mp4, "video/mp4");

        Assert.Equal(6.0, info.DurationSeconds);
        Assert.Equal(640, info.Width);
    }

    [Fact]
    public void Mp4_WithoutAMovieBox_IsEmpty()
    {
        Assert.Equal(MediaMetadata.Empty, Read(Join(Box("ftyp", Ascii("isom"), new byte[8]), Box("mdat", new byte[10])), "video/mp4"));
    }

    // ----- WebM

    private static byte[] El(byte[] id, params byte[][] payload)
    {
        var body = Join(payload);
        return Join(id, new byte[] { (byte)(0x80 | body.Length) }, body);
    }

    [Fact]
    public void Webm_DurationUsesTheTimecodeScale_AndSizeComesFromTheVideoTrack()
    {
        var duration = El(new byte[] { 0x44, 0x89 }, BitConverter.GetBytes(30_000f).Reverse().ToArray()); // 30 000 ticks
        var scale = El(new byte[] { 0x2A, 0xD7, 0xB1 }, Be32(1_000_000)); // 1 ms per tick
        var info = El(new byte[] { 0x15, 0x49, 0xA9, 0x66 }, scale, duration);
        var video = El(new byte[] { 0xE0 }, El(new byte[] { 0xB0 }, Be16(1280)), El(new byte[] { 0xBA }, Be16(720)));
        var tracks = El(new byte[] { 0x16, 0x54, 0xAE, 0x6B }, El(new byte[] { 0xAE }, video));
        var segment = El(new byte[] { 0x18, 0x53, 0x80, 0x67 }, info, tracks);
        var webm = Join(El(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, new byte[3]), segment);

        var result = Read(webm, "video/webm");

        Assert.Equal(30.0, result.DurationSeconds);
        Assert.Equal(1280, result.Width);
        Assert.Equal(720, result.Height);
    }

    // ----- whatever else

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/webp")]
    [InlineData("video/mp4")]
    [InlineData("video/webm")]
    [InlineData("text/html")]
    public void Garbage_AndTruncatedFiles_GiveEmptyInsteadOfThrowing(string type)
    {
        Assert.Equal(MediaMetadata.Empty, Read(Array.Empty<byte>(), type));
        Assert.Equal(MediaMetadata.Empty, Read(new byte[] { 1, 2, 3 }, type));
        Assert.Equal(MediaMetadata.Empty, Read(Enumerable.Repeat((byte)0xFF, 400).ToArray(), type));
    }
}
