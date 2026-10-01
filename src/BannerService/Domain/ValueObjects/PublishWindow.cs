namespace BannerService.Domain.ValueObjects;

/// <summary>Half-open UTC interval [Start, End) during which a banner is shown.</summary>
public class PublishWindow
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public PublishWindow(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new ArgumentException("Publish end must be after publish start");

        Start = start;
        End = end;
    }

    public bool Contains(DateTime moment) => moment >= Start && moment < End;

    public bool Overlaps(PublishWindow other) => Start < other.End && other.Start < End;

    public bool IsSameDay => Start.Date == End.AddTicks(-1).Date;
}
