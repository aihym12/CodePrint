namespace CodePrint.Models;

public class DateElement : LabelElement
{
    private string _dateFormat = "yyyy-MM-dd";
    private int _dayOffset;
    private string _fontFamily = "Microsoft YaHei";
    private double _fontSize = 10;
    private string _foregroundColor = "#000000";

    public DateElement() { Type = ElementType.Date; Name = "日期"; Width = 25; Height = 5; }

    public string DateFormat { get => _dateFormat; set => SetField(ref _dateFormat, value); }
    public int DayOffset { get => _dayOffset; set => SetField(ref _dayOffset, value); }
    public string FontFamily { get => _fontFamily; set => SetField(ref _fontFamily, value); }
    public double FontSize { get => _fontSize; set => SetField(ref _fontSize, value); }
    public string ForegroundColor { get => _foregroundColor; set => SetField(ref _foregroundColor, value); }
}
