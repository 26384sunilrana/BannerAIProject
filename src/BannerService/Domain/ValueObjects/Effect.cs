namespace BannerService.Domain.ValueObjects;

public class Effect
{
    public Guid Id { get; set; }
    public Guid ComponentId { get; set; }
    public int EffectType { get; set; }  // 1=Opacity, 2=Rotation, 3=Scale, 4=Blur, 5=Animation
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Effect() { }

    public Effect(int effectType, Dictionary<string, object> parameters)
    {
        if (effectType < 1 || effectType > 5)
            throw new ArgumentException("EffectType must be 1-5");

        Id = Guid.NewGuid();
        EffectType = effectType;
        Parameters = parameters ?? new Dictionary<string, object>();
        CreatedAt = DateTime.UtcNow;
        IsEnabled = true;
    }
}
