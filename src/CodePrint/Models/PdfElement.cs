namespace CodePrint.Models;

public class PdfElement : LabelElement
{
    private string _filePath = string.Empty;
    private int _pageNumber = 1;
    private int _pageRangeStart = 1;
    private int _pageRangeEnd = 1;

    public PdfElement() { Type = ElementType.Pdf; Name = "PDF"; Width = 30; Height = 20; }

    public string FilePath { get => _filePath; set => SetField(ref _filePath, value); }
    public int PageNumber { get => _pageNumber; set => SetField(ref _pageNumber, value); }
    public int PageRangeStart { get => _pageRangeStart; set => SetField(ref _pageRangeStart, value); }
    public int PageRangeEnd { get => _pageRangeEnd; set => SetField(ref _pageRangeEnd, value); }
}
