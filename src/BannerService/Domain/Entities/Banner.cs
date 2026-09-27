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
