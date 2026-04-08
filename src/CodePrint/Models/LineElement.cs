namespace CodePrint.Models;

public enum LineStyle { Solid, Dashed, Dotted }
public enum LineEndCap { Flat, Round, Arrow }

public class LineElement : LabelElement
{
    private double _strokeThickness = 1;
    private string _strokeColor = "#000000";
    private LineStyle _style = LineStyle.Solid;
    private LineEndCap _startCap = LineEndCap.Flat;
    private LineEndCap _endCap = LineEndCap.Flat;
    private double _x2;
    private double _y2;

    public LineElement() { Type = ElementType.Line; Name = "线条"; Width = 30; Height = 1; }

    public double StrokeThickness { get => _strokeThickness; set => SetField(ref _strokeThickness, value); }
    public string StrokeColor { get => _strokeColor; set => SetField(ref _strokeColor, value); }
    public LineStyle Style { get => _style; set => SetField(ref _style, value); }
    public LineEndCap StartCap { get => _startCap; set => SetField(ref _startCap, value); }
    public LineEndCap EndCap { get => _endCap; set => SetField(ref _endCap, value); }
    public double X2 { get => _x2; set => SetField(ref _x2, value); }
    public double Y2 { get => _y2; set => SetField(ref _y2, value); }
}
