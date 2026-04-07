namespace CodePrint.Models;

public class DateElement : LabelElement
{
    public DateElement() { Type = ElementType.Date; Name = "日期"; Width = 25; Height = 5; }
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public int DayOffset { get; set; }
    public string FontFamily { get; set; } = "Microsoft YaHei";
    public double FontSize { get; set; } = 10;
    public string ForegroundColor { get; set; } = "#000000";
}
