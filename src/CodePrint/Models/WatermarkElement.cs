namespace CodePrint.Models;

public class WatermarkElement : LabelElement
{
    private string _text = "水印";
    private string _fontFamily = "Microsoft YaHei";
    private double _fontSize = 24;
    private string _color = "#000000";
    private double _angle = -45;
    private double _spacing = 50;

    public WatermarkElement() { Type = ElementType.Watermark; Name = "水印"; Opacity = 0.15; }

    public string Text { get => _text; set => SetField(ref _text, value); }
    public string FontFamily { get => _fontFamily; set => SetField(ref _fontFamily, value); }
    public double FontSize { get => _fontSize; set => SetField(ref _fontSize, value); }
    public string Color { get => _color; set => SetField(ref _color, value); }
    public double Angle { get => _angle; set => SetField(ref _angle, value); }
    public double Spacing { get => _spacing; set => SetField(ref _spacing, value); }
}
