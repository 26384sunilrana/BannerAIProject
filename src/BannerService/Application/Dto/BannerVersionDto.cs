namespace BannerService.Application.Dto;

public class BannerVersionDto
{
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? ChangeDescription { get; set; }
    public bool IsActive { get; set; }
    public int ComponentCount { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string BannerName { get; set; } = string.Empty;
}

public class BannerVersionDetailDto
{
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? ChangeDescription { get; set; }
    public string BannerName { get; set; } = string.Empty;
    public string BannerDescription { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public List<PreviewComponentDto> Components { get; set; } = new();
}

public class RestoreVersionRequestDto
{
    public int VersionNumber { get; set; }
}
