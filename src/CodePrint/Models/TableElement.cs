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
    public TableElement() { Type = ElementType.Table; Name = "表格"; Width = 40; Height = 30; }
    public int Rows { get; set; } = 3;
    public int Columns { get; set; } = 3;
    public List<TableCell> Cells { get; set; } = new();
    public string BorderColor { get; set; } = "#000000";
    public double BorderThickness { get; set; } = 1;
}
