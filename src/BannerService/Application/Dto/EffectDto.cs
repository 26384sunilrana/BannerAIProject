namespace BannerService.Application.Dto;

public class EffectDto
{
    public Guid Id { get; set; }
    public int EffectType { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApplyEffectRequestDto
{
    public int EffectType { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class UpdateEffectRequestDto
{
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class CarouselDto
{
    public Guid Id { get; set; }
    public int IntervalMs { get; set; }
    public int TransitionDuration { get; set; }
    public int TransitionType { get; set; }
    public bool IsAutoplay { get; set; }
    public bool Loop { get; set; }
    public List<Guid> ComponentIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateCarouselRequestDto
{
    public int IntervalMs { get; set; }
    public int TransitionDuration { get; set; }
    public int TransitionType { get; set; }
    public List<Guid> ComponentIds { get; set; } = new();
    public bool IsAutoplay { get; set; } = true;
    public bool Loop { get; set; } = true;
}
