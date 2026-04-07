namespace CodePrint.Models;

public enum TemplateCategory
{
    ECommerce,
    ProductLabel,
    Food,
    Retail,
    Industrial,
    Office
}

public class LabelTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TemplateCategory Category { get; set; }
    public string? ThumbnailPath { get; set; }
    public LabelDocument Document { get; set; } = new();
    public bool IsOfficial { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
