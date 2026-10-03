namespace BannerService.Domain.Services
{
    using ValueObjects;

    /// <summary>A stretch of time (UTC) during which a banner is shown.</summary>
    public readonly record struct Occurrence(DateTime Start, DateTime End)
    {
        public bool Contains(DateTime moment) => moment >= Start && moment < End;
        public bool Overlaps(Occurrence other) => Start < other.End && other.Start < End;
    }

    /// <summary>Time zones by their IANA names (Asia/Kolkata), as the shop and its city carry them.</summary>
    public static class TimeZones
    {
        public static bool TryGet(string? id, out TimeZoneInfo zone)
        {
            zone = TimeZoneInfo.Utc;
            if (string.IsNullOrWhiteSpace(id) || id.Length > 100)
                return false;

            return TimeZoneInfo.TryFindSystemTimeZoneById(id.Trim(), out zone!);
        }
    }

    /// <summary>
    /// Works out when a banner really is shown: its date window, narrowed to the daily hours and weekdays in the shop's time zone.
    /// Overlap checks and "what is live now" both use this, so they always agree.
    /// </summary>
    public static class ScheduleOccurrences
    {
        /// <summary>A safety stop: no check ever walks more days than this.</summary>
        public const int MaxDays = 4000;

        /// <summary>The stretches inside [fromUtc, toUtc) when the banner is shown, earliest first.</summary>
        public static List<Occurrence> For(PublishWindow window, DailySchedule? daily, TimeZoneInfo zone, DateTime fromUtc, DateTime toUtc)
        {
            var start = Max(window.Start, fromUtc);
            var end = Min(window.End, toUtc);
            var result = new List<Occurrence>();
            if (end <= start)
                return result;

            if (daily == null)
            {
                result.Add(new Occurrence(start, end));
                return result;
            }

            // Walk the shop's calendar days from the one before the range (an overnight window may still be running) to the last one in it
            var firstDay = ToLocal(start, zone).Date.AddDays(-1);
            var lastDay = ToLocal(end, zone).Date;
            if ((lastDay - firstDay).TotalDays > MaxDays)
                throw new ArgumentException("That date range is too long to check");

            for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
            {
                if (!daily.Includes(day.DayOfWeek))
                    continue;

                var from = ToUtc(day.AddMinutes(daily.StartMinutes), zone);
                var to = ToUtc(day.AddMinutes(daily.EndMinutes).AddDays(daily.IsOvernight ? 1 : 0), zone);
                var clippedStart = Max(from, start);
                var clippedEnd = Min(to, end);
                if (clippedEnd > clippedStart)
                    result.Add(new Occurrence(clippedStart, clippedEnd));
            }

            return result;
        }

        /// <summary>Whether the banner is being shown at this moment.</summary>
        public static bool IsLive(PublishWindow window, DailySchedule? daily, TimeZoneInfo zone, DateTime nowUtc)
        {
            if (!window.Contains(nowUtc))
                return false;
            if (daily == null)
                return true;

            return For(window, daily, zone, nowUtc.AddDays(-2), nowUtc.AddDays(2)).Any(o => o.Contains(nowUtc));
        }

        /// <summary>True when the two banners would be shown at the same moment at some point.</summary>
        public static bool Conflict(
            PublishWindow a, DailySchedule? dailyA, PublishWindow b, DailySchedule? dailyB, TimeZoneInfo zone)
        {
            var from = Max(a.Start, b.Start);
            var to = Min(a.End, b.End);
            if (to <= from)
                return false;

            var first = For(a, dailyA, zone, from, to);
            var second = For(b, dailyB, zone, from, to);

            // both lists are in time order: walk them together
            int i = 0, j = 0;
            while (i < first.Count && j < second.Count)
            {
                if (first[i].Overlaps(second[j]))
                    return true;
                if (first[i].End <= second[j].End) i++; else j++;
            }
            return false;
        }

        // ----- local time

        private static DateTime ToLocal(DateTime utc, TimeZoneInfo zone) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);

        /// <summary>A wall-clock time in the zone as a UTC moment. A time that does not exist (clocks jump forward) moves on an hour; one that happens twice takes the first.</summary>
        private static DateTime ToUtc(DateTime local, TimeZoneInfo zone)
        {
            local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(local))
                local = local.AddHours(1);

            if (zone.IsAmbiguousTime(local))
            {
                var offset = zone.GetAmbiguousTimeOffsets(local).Max();
                return DateTime.SpecifyKind(local - offset, DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeToUtc(local, zone);
        }

        private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;
        private static DateTime Min(DateTime a, DateTime b) => a <= b ? a : b;
    }
}
