namespace BannerService.Domain.ValueObjects
{
    /// <summary>
    /// Hours of the day (in the shop's own time zone) during which a banner is shown on each day of its date window, and on
    /// which weekdays. Minutes count from midnight; when the end is not after the start the hours run past midnight
    /// (22:00 to 02:00). The weekday of an overnight window is the day it starts.
    /// </summary>
    public sealed record DailySchedule(int StartMinutes, int EndMinutes, int Days = DailySchedule.AllDays)
    {
        /// <summary>One bit per weekday, using the .NET numbering: bit 0 Sunday ... bit 6 Saturday.</summary>
        public const int AllDays = 0b1111111;

        public const int MinutesPerDay = 24 * 60;

        public static int Bit(DayOfWeek day) => 1 << (int)day;

        public bool Includes(DayOfWeek day) => (Days & Bit(day)) != 0;

        /// <summary>True when the hours cross midnight.</summary>
        public bool IsOvernight => EndMinutes <= StartMinutes;

        public void Validate()
        {
            if (StartMinutes < 0 || StartMinutes >= MinutesPerDay || EndMinutes < 0 || EndMinutes >= MinutesPerDay)
                throw new ArgumentException("Daily hours must be between 00:00 and 23:59");
            if (StartMinutes == EndMinutes)
                throw new ArgumentException("The daily start and end cannot be the same time");
            if ((Days & AllDays) == 0 || (Days & ~AllDays) != 0)
                throw new ArgumentException("Choose at least one day of the week");
        }
    }
}
