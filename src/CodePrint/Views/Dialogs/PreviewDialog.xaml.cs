using System.Windows;
using CodePrint.Helpers;
using CodePrint.Models;

namespace CodePrint.Views.Dialogs;

public partial class PreviewDialog : Window
{
    public PreviewDialog(LabelDocument document)
    {
        InitializeComponent();

        var widthPx = document.WidthMm * DesignConstants.MmToPixel;
        var heightPx = document.HeightMm * DesignConstants.MmToPixel;

        PreviewCanvas.Width = widthPx;
        PreviewCanvas.Height = heightPx;

        SizeText.Text = $"{document.WidthMm} × {document.HeightMm} mm";

        // Render elements as simple visual representations
        foreach (var element in document.Elements)
        {
            if (!element.IsVisible) continue;
            CanvasRendererHelper.RenderElement(PreviewCanvas, element);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
