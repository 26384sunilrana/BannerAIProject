namespace BannerService.Domain.ValueObjects;

public class LayerOrder
{
    public Guid ComponentId { get; set; }
    public int OldZIndex { get; set; }
    public int NewZIndex { get; set; }
    public string Reason { get; set; } = "reorder";

    public LayerOrder() { }

    public LayerOrder(Guid componentId, int oldZIndex, int newZIndex, string reason = "reorder")
    {
        if (oldZIndex == newZIndex)
            throw new ArgumentException("OldZIndex and NewZIndex must be different");

        if (oldZIndex < 0 || oldZIndex > 100 || newZIndex < 0 || newZIndex > 100)
            throw new ArgumentException("ZIndex values must be 0-100");

        ComponentId = componentId;
        OldZIndex = oldZIndex;
        NewZIndex = newZIndex;
        Reason = reason;
    }
}
