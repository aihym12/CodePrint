namespace CodePrint.Models;

public enum TextAlignment { Left, Center, Right, Justify }
public enum TextDirection { Horizontal, Vertical }

public class TextElement : LabelElement
{
    private string _content = "文本";
    private string _fontFamily = "Microsoft YaHei";
    private double _fontSize = 12;
    private bool _isBold;
    private bool _isItalic;
    private bool _isUnderline;
    private bool _isStrikethrough;
    private string _foregroundColor = "#000000";
    private string _backgroundColor = "Transparent";
    private TextAlignment _textAlignment = TextAlignment.Left;
    private double _letterSpacing;
    private double _lineSpacing = 1.2;
    private TextDirection _direction = TextDirection.Horizontal;
    private bool _isMultiline;

    public TextElement() { Type = ElementType.Text; Name = "文本"; Width = 25; Height = 8; }

    public string Content { get => _content; set => SetField(ref _content, value); }
    public string FontFamily { get => _fontFamily; set => SetField(ref _fontFamily, value); }
    public double FontSize { get => _fontSize; set => SetField(ref _fontSize, value); }
    public bool IsBold { get => _isBold; set => SetField(ref _isBold, value); }
    public bool IsItalic { get => _isItalic; set => SetField(ref _isItalic, value); }
    public bool IsUnderline { get => _isUnderline; set => SetField(ref _isUnderline, value); }
    public bool IsStrikethrough { get => _isStrikethrough; set => SetField(ref _isStrikethrough, value); }
    public string ForegroundColor { get => _foregroundColor; set => SetField(ref _foregroundColor, value); }
    public string BackgroundColor { get => _backgroundColor; set => SetField(ref _backgroundColor, value); }
    public TextAlignment TextAlignment { get => _textAlignment; set => SetField(ref _textAlignment, value); }
    public double LetterSpacing { get => _letterSpacing; set => SetField(ref _letterSpacing, value); }
    public double LineSpacing { get => _lineSpacing; set => SetField(ref _lineSpacing, value); }
    public TextDirection Direction { get => _direction; set => SetField(ref _direction, value); }
    public bool IsMultiline { get => _isMultiline; set => SetField(ref _isMultiline, value); }
}
