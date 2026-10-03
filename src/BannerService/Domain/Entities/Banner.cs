namespace BannerService.Domain.Entities;

using ValueObjects;

public class Banner
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishStartAt { get; private set; }
    public DateTime? PublishEndAt { get; private set; }

    /// <summary>Hours of the day (shop time) the banner is shown within its date window; null means the whole window.</summary>
    public int? DailyStartMinutes { get; private set; }
    public int? DailyEndMinutes { get; private set; }

    /// <summary>Weekdays the daily hours apply to (see <see cref="DailySchedule.Bit"/>); all days when no daily hours are set.</summary>
    public int ActiveDays { get; private set; } = DailySchedule.AllDays;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    private List<Component> _components = new();
    public IReadOnlyCollection<Component> Components => _components.AsReadOnly();

    public Banner() { }

    public Banner(Guid shopId, Guid userId, string name, string description, int width, int height)
    {
        Id = Guid.NewGuid();
        ShopId = shopId;
        UserId = userId;
        Name = name;
        Description = description;
        Width = width;
        Height = height;
        IsPublished = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddComponent(Component component)
    {
        if (_components.Count >= 50)
            throw new InvalidOperationException("Cannot add more than 50 components to a banner");

        if (_components.Any(c => c.ZIndex == component.ZIndex))
            throw new InvalidOperationException($"Component with ZIndex {component.ZIndex} already exists");

        _components.Add(component);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveComponent(Guid componentId)
    {
        var component = _components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException($"Component {componentId} not found");

        _components.Remove(component);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateComponent(Guid componentId, Position position, Size size, int zIndex, string propertiesJson)
    {
        var component = _components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException($"Component {componentId} not found");

        // Check ZIndex uniqueness
        if (component.ZIndex != zIndex && _components.Any(c => c.ZIndex == zIndex && c.Id != componentId))
            throw new InvalidOperationException($"Component with ZIndex {zIndex} already exists");

        component.Update(position, size, zIndex, propertiesJson);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SwapComponentZIndexes(Guid firstId, Guid secondId)
    {
        var first = _components.FirstOrDefault(c => c.Id == firstId)
            ?? throw new InvalidOperationException($"Component {firstId} not found");
        var second = _components.FirstOrDefault(c => c.Id == secondId)
            ?? throw new InvalidOperationException($"Component {secondId} not found");

        var firstZ = first.ZIndex;
        first.Update(new Position(first.PositionX, first.PositionY), new Size(first.SizeWidth, first.SizeHeight), second.ZIndex, first.PropertiesJson);
        second.Update(new Position(second.PositionX, second.PositionY), new Size(second.SizeWidth, second.SizeHeight), firstZ, second.PropertiesJson);
        UpdatedAt = DateTime.UtcNow;
    }

    public PublishWindow? GetPublishWindow() =>
        PublishStartAt.HasValue && PublishEndAt.HasValue ? new PublishWindow(PublishStartAt.Value, PublishEndAt.Value) : null;

    public DailySchedule? GetDailySchedule() =>
        DailyStartMinutes.HasValue && DailyEndMinutes.HasValue
            ? new DailySchedule(DailyStartMinutes.Value, DailyEndMinutes.Value, ActiveDays)
            : null;

    /// <summary>
    /// Sets the publish window and, optionally, the daily hours inside it. Returns true when the change must go
    /// through approval again (a schedule that was already set was changed in any way).
    /// </summary>
    public bool SetSchedule(PublishWindow window, DailySchedule? daily = null)
    {
        daily?.Validate();

        var changed = PublishStartAt != window.Start || PublishEndAt != window.End || GetDailySchedule() != daily;
        var hadSchedule = PublishStartAt.HasValue;

        PublishStartAt = window.Start;
        PublishEndAt = window.End;
        DailyStartMinutes = daily?.StartMinutes;
        DailyEndMinutes = daily?.EndMinutes;
        ActiveDays = daily?.Days ?? DailySchedule.AllDays;
        UpdatedAt = DateTime.UtcNow;

        if (!changed || !hadSchedule)
            return false;

        IsPublished = false;
        return true;
    }

    public void Publish()
    {
        IsPublished = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
}
