namespace CodePrint.Models;

public class PdfElement : LabelElement
{
    public PdfElement() { Type = ElementType.Pdf; Name = "PDF"; Width = 30; Height = 20; }
    public string FilePath { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageRangeStart { get; set; } = 1;
    public int PageRangeEnd { get; set; } = 1;
}
