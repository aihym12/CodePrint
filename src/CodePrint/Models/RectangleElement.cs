namespace CodePrint.Models;

public class RectangleElement : LabelElement
{
    public RectangleElement() { Type = ElementType.Rectangle; Name = "矩形"; Width = 20; Height = 15; }
    public string FillColor { get; set; } = "Transparent";
    public string BorderColor { get; set; } = "#000000";
    public double BorderThickness { get; set; } = 1;
    public LineStyle BorderStyle { get; set; } = LineStyle.Solid;
    public double CornerRadius { get; set; }
    public bool HasShadow { get; set; }
}
