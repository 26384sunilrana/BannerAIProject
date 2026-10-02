namespace BannerService.Domain.Services
{
    /// <summary>What can be read from a file's header: picture size, and for a video also its length.</summary>
    public record MediaMetadata(int? Width, int? Height, double? DurationSeconds)
    {
        public static readonly MediaMetadata Empty = new(null, null, null);
    }

    /// <summary>
    /// Reads picture size and video length straight from the file headers (PNG, JPEG, GIF, WebP, MP4, WebM), so no outside
    /// tool is needed. A file that cannot be understood gives <see cref="MediaMetadata.Empty"/>; it is never an error,
    /// because the type and signature were already checked when the upload was completed.
    /// </summary>
    public static class MediaMetadataReader
    {
        public static MediaMetadata Read(Stream stream, string contentType)
        {
            try
            {
                return contentType.ToLowerInvariant() switch
                {
                    "image/png" => ReadPng(stream),
                    "image/gif" => ReadGif(stream),
                    "image/jpeg" => ReadJpeg(stream),
                    "image/webp" => ReadWebp(stream),
                    "video/mp4" => ReadMp4(stream),
                    "video/webm" => ReadWebm(stream),
                    _ => MediaMetadata.Empty,
                };
            }
            catch (Exception ex) when (ex is EndOfStreamException or IOException or ArgumentException or OverflowException or InvalidDataException)
            {
                return MediaMetadata.Empty;
            }
        }

        // ----- helpers

        private static byte[] Take(Stream s, int count)
        {
            var buffer = new byte[count];
            var read = 0;
            while (read < count)
            {
                var n = s.Read(buffer, read, count - read);
                if (n == 0) throw new EndOfStreamException();
                read += n;
            }
            return buffer;
        }

        private static uint U32Be(byte[] b, int o) => (uint)(b[o] << 24 | b[o + 1] << 16 | b[o + 2] << 8 | b[o + 3]);
        private static ulong U64Be(byte[] b, int o) => ((ulong)U32Be(b, o) << 32) | U32Be(b, o + 4);
        private static int U16Be(byte[] b, int o) => b[o] << 8 | b[o + 1];
        private static int U16Le(byte[] b, int o) => b[o] | b[o + 1] << 8;

        private static MediaMetadata Size(long width, long height) =>
            width is > 0 and <= 65535 && height is > 0 and <= 65535 ? new((int)width, (int)height, null) : MediaMetadata.Empty;

        // ----- pictures

        private static MediaMetadata ReadPng(Stream s)
        {
            var header = Take(s, 24); // signature, then the IHDR chunk: length, "IHDR", width, height
            return Size(U32Be(header, 16), U32Be(header, 20));
        }

        private static MediaMetadata ReadGif(Stream s)
        {
            var header = Take(s, 10);
            return Size(U16Le(header, 6), U16Le(header, 8));
        }

        private static MediaMetadata ReadJpeg(Stream s)
        {
            var start = Take(s, 2);
            if (start[0] != 0xFF || start[1] != 0xD8) return MediaMetadata.Empty;

            while (true)
            {
                // find the next marker: 0xFF followed by a code that is not 0x00 or 0xFF padding
                int b;
                do { b = s.ReadByte(); if (b < 0) return MediaMetadata.Empty; } while (b != 0xFF);
                int code;
                do { code = s.ReadByte(); if (code < 0) return MediaMetadata.Empty; } while (code == 0xFF);

                if (code == 0x00 || code == 0x01 || (code >= 0xD0 && code <= 0xD9)) continue; // no length follows

                var length = U16Be(Take(s, 2), 0);
                if (length < 2) return MediaMetadata.Empty;

                var isFrame = code is >= 0xC0 and <= 0xCF && code != 0xC4 && code != 0xC8 && code != 0xCC;
                if (isFrame)
                {
                    var frame = Take(s, 5); // precision, height, width
                    return Size(U16Be(frame, 3), U16Be(frame, 1));
                }

                s.Seek(length - 2, SeekOrigin.Current);
            }
        }

        private static MediaMetadata ReadWebp(Stream s)
        {
            var head = Take(s, 30);
            if (head[0] != 'R' || head[8] != 'W' || head[9] != 'E' || head[10] != 'B' || head[11] != 'P') return MediaMetadata.Empty;

            var kind = System.Text.Encoding.ASCII.GetString(head, 12, 4);
            switch (kind)
            {
                case "VP8X": // 24-bit width-1 and height-1 at 24 and 27
                    return Size((head[24] | head[25] << 8 | head[26] << 16) + 1L, (head[27] | head[28] << 8 | head[29] << 16) + 1L);
                case "VP8L": // 0x2F signature then 14 bits width-1, 14 bits height-1
                {
                    var bits = (uint)(head[21] | head[22] << 8 | head[23] << 16 | head[24] << 24);
                    return Size((bits & 0x3FFF) + 1L, ((bits >> 14) & 0x3FFF) + 1L);
                }
                case "VP8 ": // frame tag (3 bytes), start code 9D 01 2A, then 14-bit width and height
                    return Size((head[26] | head[27] << 8) & 0x3FFF, (head[28] | head[29] << 8) & 0x3FFF);
                default:
                    return MediaMetadata.Empty;
            }
        }

        // ----- MP4

        private static MediaMetadata ReadMp4(Stream s)
        {
            double? duration = null;
            int? width = null, height = null;

            WalkBoxes(s, 0, s.Length, 0, (type, payloadStart) =>
            {
                switch (type)
                {
                    case "moov":
                        return true; // look inside
                    case "mvhd":
                        duration ??= ReadMvhd(s, payloadStart);
                        return false;
                    case "trak":
                        return width == null; // only the first picture track matters
                    case "tkhd":
                        var (w, h) = ReadTkhd(s, payloadStart);
                        if (w > 0 && h > 0) { width = w; height = h; }
                        return false;
                    default:
                        return false;
                }
            });

            return new MediaMetadata(width, height, duration);
        }

        /// <summary>Visits each box between start and end; when the visitor returns true the box is opened and its children are visited.</summary>
        private static void WalkBoxes(Stream s, long start, long end, int depth, Func<string, long, bool> visit)
        {
            if (depth > 6) return;
            var position = start;
            for (var guard = 0; guard < 10_000 && position + 8 <= end; guard++)
            {
                s.Seek(position, SeekOrigin.Begin);
                var head = Take(s, 8);
                long size = U32Be(head, 0);
                var type = System.Text.Encoding.ASCII.GetString(head, 4, 4);
                var payload = position + 8;
                if (size == 1) { size = (long)U64Be(Take(s, 8), 0); payload += 8; }
                else if (size == 0) size = end - position;

                if (position + size > end) size = end - position; // a box may not run past its parent
                if (size < 8) return;

                var boxEnd = position + size;
                if (visit(type, payload))
                    WalkBoxes(s, payload, boxEnd, depth + 1, visit);
                position = boxEnd;
            }
        }

        private static double? ReadMvhd(Stream s, long payload)
        {
            s.Seek(payload, SeekOrigin.Begin);
            var version = Take(s, 4)[0];
            if (version == 1)
            {
                var b = Take(s, 28); // created 8, modified 8, timescale 4, duration 8
                var scale = U32Be(b, 16);
                var dur = U64Be(b, 20);
                return scale == 0 ? null : (double)dur / scale;
            }
            else
            {
                var b = Take(s, 16); // created 4, modified 4, timescale 4, duration 4
                var scale = U32Be(b, 8);
                var dur = U32Be(b, 12);
                return scale == 0 ? null : (double)dur / scale;
            }
        }

        private static (int width, int height) ReadTkhd(Stream s, long payload)
        {
            s.Seek(payload, SeekOrigin.Begin);
            var version = Take(s, 4)[0];
            s.Seek(payload + (version == 1 ? 88 : 76), SeekOrigin.Begin);
            var b = Take(s, 8);
            return ((int)(U32Be(b, 0) >> 16), (int)(U32Be(b, 4) >> 16)); // 16.16 fixed point
        }

        // ----- WebM

        private const uint Segment = 0x18538067, Info = 0x1549A966, Tracks = 0x1654AE6B, TrackEntry = 0xAE, VideoSettings = 0xE0;
        private const uint TimecodeScaleId = 0x2AD7B1, DurationId = 0x4489, PixelWidth = 0xB0, PixelHeight = 0xBA, Cluster = 0x1F43B675;

        private static MediaMetadata ReadWebm(Stream s)
        {
            double? rawDuration = null;
            long scale = 1_000_000; // nanoseconds per tick unless the file says otherwise
            int? width = null, height = null;
            var seen = 0;

            void Walk(long end, int depth)
            {
                while (s.Position < end && seen++ < 5000)
                {
                    if (!TryReadId(s, out var id) || !TryReadSize(s, out var size, out var unknown)) return;
                    var dataStart = s.Position;
                    var dataEnd = unknown ? end : Math.Min(end, dataStart + size);

                    if (id == Cluster) { s.Seek(end, SeekOrigin.Begin); return; } // everything needed comes before the first cluster

                    if (id is Segment or Info or Tracks or TrackEntry or VideoSettings && depth < 6)
                    {
                        Walk(dataEnd, depth + 1);
                    }
                    else if (id == TimecodeScaleId) scale = (long)ReadUnsigned(s, (int)size);
                    else if (id == DurationId) rawDuration = ReadFloat(s, (int)size);
                    else if (id == PixelWidth && width == null) width = (int)ReadUnsigned(s, (int)size);
                    else if (id == PixelHeight && height == null) height = (int)ReadUnsigned(s, (int)size);

                    s.Seek(dataEnd, SeekOrigin.Begin);
                }
            }

            Walk(s.Length, 0);
            double? seconds = rawDuration is > 0 ? rawDuration * scale / 1_000_000_000d : null;
            return new MediaMetadata(width, height, seconds);
        }

        private static bool TryReadId(Stream s, out uint id)
        {
            id = 0;
            var first = s.ReadByte();
            if (first <= 0) return false;
            var length = first >= 0x80 ? 1 : first >= 0x40 ? 2 : first >= 0x20 ? 3 : first >= 0x10 ? 4 : 0;
            if (length == 0) return false;
            id = (uint)first;
            for (var i = 1; i < length; i++)
            {
                var b = s.ReadByte();
                if (b < 0) return false;
                id = id << 8 | (uint)b;
            }
            return true;
        }

        private static bool TryReadSize(Stream s, out long size, out bool unknown)
        {
            size = 0; unknown = false;
            var first = s.ReadByte();
            if (first <= 0) return false;
            var length = 1;
            var mask = 0x80;
            while (length <= 8 && (first & mask) == 0) { length++; mask >>= 1; }
            if (length > 8) return false;

            size = first & (mask - 1);
            var allOnes = size == mask - 1;
            for (var i = 1; i < length; i++)
            {
                var b = s.ReadByte();
                if (b < 0) return false;
                size = size << 8 | (uint)b;
                allOnes &= b == 0xFF;
            }
            unknown = allOnes;
            return true;
        }

        private static ulong ReadUnsigned(Stream s, int length)
        {
            if (length is < 1 or > 8) throw new InvalidDataException();
            ulong value = 0;
            foreach (var b in Take(s, length)) value = value << 8 | b;
            return value;
        }

        private static double ReadFloat(Stream s, int length)
        {
            var bytes = Take(s, length);
            if (length == 4) return BitConverter.ToSingle(new[] { bytes[3], bytes[2], bytes[1], bytes[0] }, 0);
            if (length == 8) return BitConverter.ToDouble(bytes.Reverse().ToArray(), 0);
            throw new InvalidDataException();
        }
    }
}
