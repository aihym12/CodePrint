namespace CodePrint.Models;

public enum PrintOrientation { Portrait, Landscape }

public class LabelDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "新建标签";
    public double WidthMm { get; set; } = 50;
    public double HeightMm { get; set; } = 30;
    public PrintOrientation Orientation { get; set; } = PrintOrientation.Portrait;
    public double MarginTop { get; set; }
    public double MarginBottom { get; set; }
    public double MarginLeft { get; set; }
    public double MarginRight { get; set; }
    public double RowSpacing { get; set; }
    public double ColumnSpacing { get; set; }
    public int ColumnsPerRow { get; set; } = 1;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public List<LabelElement> Elements { get; set; } = new();
    public string? FolderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    public string? ThumbnailPath { get; set; }
}
