namespace CodePrint.Models;

public class RectangleElement : LabelElement
{
    private string _fillColor = "Transparent";
    private string _borderColor = "#000000";
    private double _borderThickness = 1;
    private LineStyle _borderStyle = LineStyle.Solid;
    private double _cornerRadius;
    private bool _hasShadow;

    public RectangleElement() { Type = ElementType.Rectangle; Name = "矩形"; Width = 20; Height = 15; }

    public string FillColor { get => _fillColor; set => SetField(ref _fillColor, value); }
    public string BorderColor { get => _borderColor; set => SetField(ref _borderColor, value); }
    public double BorderThickness { get => _borderThickness; set => SetField(ref _borderThickness, value); }
    public LineStyle BorderStyle { get => _borderStyle; set => SetField(ref _borderStyle, value); }
    public double CornerRadius { get => _cornerRadius; set => SetField(ref _cornerRadius, value); }
    public bool HasShadow { get => _hasShadow; set => SetField(ref _hasShadow, value); }
}
