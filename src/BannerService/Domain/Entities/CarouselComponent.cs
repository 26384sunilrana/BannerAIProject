namespace BannerService.Domain.Entities;

public class CarouselComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CarouselId { get; set; }
    public Guid ComponentId { get; set; }
    public int Order { get; set; }
}