namespace CodePrint.Models;

public enum LineStyle { Solid, Dashed, Dotted }
public enum LineEndCap { Flat, Round, Arrow }

public class LineElement : LabelElement
{
    public LineElement() { Type = ElementType.Line; Name = "线条"; Width = 30; Height = 1; }
    public double StrokeThickness { get; set; } = 1;
    public string StrokeColor { get; set; } = "#000000";
    public LineStyle Style { get; set; } = LineStyle.Solid;
    public LineEndCap StartCap { get; set; } = LineEndCap.Flat;
    public LineEndCap EndCap { get; set; } = LineEndCap.Flat;
    public double X2 { get; set; }
    public double Y2 { get; set; }
}
