namespace CodePrint.Models;

public class WatermarkElement : LabelElement
{
    public WatermarkElement() { Type = ElementType.Watermark; Name = "水印"; Opacity = 0.15; }
    public string Text { get; set; } = "水印";
    public string FontFamily { get; set; } = "Microsoft YaHei";
    public double FontSize { get; set; } = 24;
    public string Color { get; set; } = "#000000";
    public double Angle { get; set; } = -45;
    public double Spacing { get; set; } = 50;
}
