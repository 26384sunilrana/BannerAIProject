namespace BannerService.Domain.ValueObjects;

public class Carousel
{
    public Guid Id { get; set; }
    public Guid BannerId { get; set; }
    public int IntervalMs { get; set; }
    public int TransitionDuration { get; set; }
    public int TransitionType { get; set; }  // 1=Fade, 2=SlideLeft, 3=SlideRight, 4=Zoom
    public bool IsAutoplay { get; set; } = true;
    public bool Loop { get; set; } = true;
    public List<Guid> ComponentIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public Carousel() { }

    public Carousel(Guid bannerId, int intervalMs, int transitionDuration,
        int transitionType, List<Guid> componentIds)
    {
        if (bannerId == Guid.Empty)
            throw new ArgumentException("BannerId cannot be empty");
        if (componentIds == null || componentIds.Count < 2)
            throw new ArgumentException("Carousel requires at least 2 components");
        if (intervalMs < 1000 || intervalMs > 30000)
            throw new ArgumentException("IntervalMs must be 1000-30000");
        if (transitionDuration < 200 || transitionDuration > 2000)
            throw new ArgumentException("TransitionDuration must be 200-2000");
        if (transitionType < 1 || transitionType > 4)
            throw new ArgumentException("TransitionType must be 1-4");

        Id = Guid.NewGuid();
        BannerId = bannerId;
        IntervalMs = intervalMs;
        TransitionDuration = transitionDuration;
        TransitionType = transitionType;
        ComponentIds = new List<Guid>(componentIds);
        CreatedAt = DateTime.UtcNow;
        IsAutoplay = true;
        Loop = true;
    }
}
