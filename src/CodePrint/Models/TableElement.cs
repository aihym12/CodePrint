namespace CodePrint.Models;

public class TableCell
{
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColSpan { get; set; } = 1;
    public string Content { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "Transparent";
}

public class TableElement : LabelElement
{
    private int _rows = 3;
    private int _columns = 3;
    private string _borderColor = "#000000";
    private double _borderThickness = 1;

    public TableElement() { Type = ElementType.Table; Name = "表格"; Width = 40; Height = 30; }

    public int Rows { get => _rows; set => SetField(ref _rows, value); }
    public int Columns { get => _columns; set => SetField(ref _columns, value); }
    public List<TableCell> Cells { get; set; } = new();
    public string BorderColor { get => _borderColor; set => SetField(ref _borderColor, value); }
    public double BorderThickness { get => _borderThickness; set => SetField(ref _borderThickness, value); }
}
