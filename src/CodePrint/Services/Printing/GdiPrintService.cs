using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodePrint.Helpers;
using CodePrint.Models;
using CodePrint.Services;

namespace CodePrint.Services.Printing;

/// <summary>
/// 基于 WPF + <c>System.Printing</c> 的打印服务实现。
/// 通过 Windows 打印 spooler 将作业交给系统驱动（GDI 路径），
/// 适合任何在系统中已安装驱动的打印机。
///
/// 注意：本实现必须在 STA UI 线程上调用，因为它构造 WPF 视觉树
/// （<see cref="FixedPage"/>/<see cref="RenderTargetBitmap"/>）和
/// 调用 <see cref="PrintDialog.PrintDocument"/>。
/// </summary>
public sealed class GdiPrintService : IPrintService
{
    public bool SupportsPrinterDiscovery => true;

    public Task<IReadOnlyList<PrinterInfo>> DiscoverPrintersAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PrinterInfo> result;
        try
        {
            using var printServer = new LocalPrintServer();
            string? defaultName = null;
            try
            {
                defaultName = LocalPrintServer.GetDefaultPrintQueue()?.Name;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GdiPrintService] 获取默认打印机失败: {ex.Message}");
            }

            var list = new List<PrinterInfo>();
            foreach (var queue in printServer.GetPrintQueues())
            {
                // 接口约定不抛异常——遇到取消用 break 而不是 ThrowIfCancellationRequested，
                // 避免用异常做控制流，并和 catch 块保持一致的"返回空/部分列表"语义。
                if (cancellationToken.IsCancellationRequested) break;
                list.Add(new PrinterInfo(
                    queue.Name,
                    IsDefault: queue.Name == defaultName,
                    IsAvailable: !queue.IsInError && !queue.IsOffline));
            }
            result = list;
        }
        catch (Exception ex)
        {
            // 受限环境（无 spooler / 无权限）下返回空列表；调用方决定是否回退。
            Debug.WriteLine($"[GdiPrintService] 枚举打印机失败: {ex}");
            result = Array.Empty<PrinterInfo>();
        }
        return Task.FromResult(result);
    }

    public async Task<PrintJobResult> PrintAsync(
        PrintJobRequest request,
        IProgress<PrintProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        // 让 UI 线程在每个阶段之间有机会刷新；同时给出取消窗口。
        progress?.Report(new PrintProgress(PrintJobStage.Preparing, "正在准备打印作业…", 0.0));
        await Task.Yield();
        if (cancellationToken.IsCancellationRequested)
            return PrintJobResult.Cancel();

        // PrintQueue 句柄需要在整个打印过程内保持有效，提前 Dispose 会出现
        // "访问已释放对象"的偶发异常，所以 server 在 finally 里释放。
        LocalPrintServer? printServer = null;
        try
        {
            var printDialog = new PrintDialog();

            try
            {
                printServer = new LocalPrintServer();
                printDialog.PrintQueue = printServer.GetPrintQueue(request.PrinterName);
            }
            catch (Exception ex)
            {
                // 找不到指定打印机时回退到系统默认打印机；记录原因便于诊断。
                Debug.WriteLine($"[GdiPrintService] 无法获取打印队列 '{request.PrinterName}': {ex.Message}");
            }

            // ── 单位说明 ──
            // WPF 打印管线统一使用"设备无关像素"（DIP，1 DIP = 1/96 英寸），
            // PrintTicket.PageMediaSize 也是 DIP 单位。mm * MmToPixel == DIP。
            var settings = request.Settings;
            var mmToDip = DesignConstants.MmToPixel;
            int rows = Math.Max(1, settings.LabelsPerRow);
            int cols = Math.Max(1, settings.LabelsPerColumn);
            double labelW = settings.PaperWidth * mmToDip;
            double labelH = settings.PaperHeight * mmToDip;
            double pageW = labelW * cols;
            double pageH = labelH * rows;

            printDialog.PrintTicket.PageMediaSize = new PageMediaSize(pageW, pageH);
            printDialog.PrintTicket.PageOrientation = settings.Orientation == PrintOrientation.Landscape
                ? System.Printing.PageOrientation.Landscape
                : System.Printing.PageOrientation.Portrait;
            printDialog.PrintTicket.CopyCount = settings.Copies;

            // 取打印机可印区原点和范围。原点（OriginWidth/Height）是相对物理页面
            // 左上角的偏移，整页只发生一次，绝不能按行列均摊。
            double originX = 0, originY = 0;
            double extentW = pageW, extentH = pageH;
            try
            {
                var caps = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
                if (caps.PageImageableArea != null)
                {
                    originX = caps.PageImageableArea.OriginWidth;
                    originY = caps.PageImageableArea.OriginHeight;
                    if (caps.PageImageableArea.ExtentWidth > 0)
                        extentW = caps.PageImageableArea.ExtentWidth;
                    if (caps.PageImageableArea.ExtentHeight > 0)
                        extentH = caps.PageImageableArea.ExtentHeight;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GdiPrintService] 读取打印机可印区失败，按零偏移处理: {ex.Message}");
            }

            progress?.Report(new PrintProgress(PrintJobStage.Rendering, "正在渲染标签内容…", 0.3));
            await Task.Yield();
            if (cancellationToken.IsCancellationRequested)
                return PrintJobResult.Cancel();

            int dpi = settings.PrintDpi > 0 ? settings.PrintDpi : AppSettingsService.Current.PrintDpi;
            var renderBitmap = RenderLabelBitmap(request.Document, dpi);

            double margin = Math.Max(0, settings.PrintMarginPx);
            double availLabelW = Math.Max(1, labelW - 2 * margin);
            double availLabelH = Math.Max(1, labelH - 2 * margin);

            var fixedPage = new FixedPage { Width = pageW, Height = pageH };

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var img = new Image
                    {
                        Source = renderBitmap,
                        Width = availLabelW,
                        Height = availLabelH,
                        Stretch = Stretch.Uniform
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

                    double left = originX + c * labelW + margin;
                    double top = originY + r * labelH + margin;

                    if (settings.MirrorPrint)
                    {
                        img.RenderTransformOrigin = new Point(0.5, 0.5);
                        img.RenderTransform = new ScaleTransform(-1, 1);
                    }

                    FixedPage.SetLeft(img, left);
                    FixedPage.SetTop(img, top);
                    fixedPage.Children.Add(img);

                    if (settings.ShowCutLines)
                    {
                        var border = new System.Windows.Shapes.Rectangle
                        {
                            Width = availLabelW,
                            Height = availLabelH,
                            Stroke = Brushes.Black,
                            StrokeThickness = 0.5,
                            StrokeDashArray = new DoubleCollection { 4, 2 },
                            Fill = Brushes.Transparent
                        };
                        FixedPage.SetLeft(border, left);
                        FixedPage.SetTop(border, top);
                        fixedPage.Children.Add(border);
                    }
                }
            }

            if (settings.EnableRegistrationPrint)
            {
                AddRegistrationMarks(fixedPage, originX, originY, extentW, extentH);
            }

            fixedPage.Measure(new Size(pageW, pageH));
            fixedPage.Arrange(new Rect(0, 0, pageW, pageH));
            fixedPage.UpdateLayout();

            var fixedDoc = new FixedDocument();
            fixedDoc.DocumentPaginator.PageSize = new Size(pageW, pageH);
            fixedDoc.Pages.Add(new PageContent { Child = fixedPage });

            progress?.Report(new PrintProgress(PrintJobStage.Spooling, "正在发送到打印机…", 0.8));
            await Task.Yield();
            if (cancellationToken.IsCancellationRequested)
                return PrintJobResult.Cancel();

            printDialog.PrintDocument(fixedDoc.DocumentPaginator, request.JobName);

            progress?.Report(new PrintProgress(PrintJobStage.Submitted, "打印任务已发送", 1.0));
            return PrintJobResult.Ok();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GdiPrintService] 打印失败: {ex}");
            progress?.Report(new PrintProgress(PrintJobStage.Failed, ex.Message, -1));
            return PrintJobResult.Fail(ex.Message);
        }
        finally
        {
            printServer?.Dispose();
        }
    }

    /// <summary>把文档渲染为高 DPI 位图，供 FixedPage 嵌入。</summary>
    private static RenderTargetBitmap RenderLabelBitmap(LabelDocument document, int dpi)
    {
        var mmToPx = DesignConstants.MmToPixel;
        double docWidth = document.WidthMm * mmToPx;
        double docHeight = document.HeightMm * mmToPx;

        // 背景色字符串来自文档（用户可编辑），有可能是非法值，必须容错。
        Brush bgBrush;
        if (string.IsNullOrEmpty(document.BackgroundColor) || document.BackgroundColor == "Transparent")
        {
            bgBrush = Brushes.White;
        }
        else
        {
            try
            {
                bgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(document.BackgroundColor));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GdiPrintService] 背景色 '{document.BackgroundColor}' 解析失败，回退为白色: {ex.Message}");
                bgBrush = Brushes.White;
            }
        }

        var canvas = new Canvas { Width = docWidth, Height = docHeight, Background = bgBrush };
        foreach (var element in document.Elements.OrderBy(e => e.ZIndex))
        {
            if (!element.IsVisible) continue;
            CanvasRendererHelper.RenderElement(canvas, element);
        }

        // 打印机使用 Grayscale 抗锯齿（ClearType 是为 LCD 子像素设计的，打印输出会糊）
        TextOptions.SetTextRenderingMode(canvas, TextRenderingMode.Grayscale);
        TextOptions.SetTextFormattingMode(canvas, TextFormattingMode.Ideal);
        TextOptions.SetTextHintingMode(canvas, TextHintingMode.Fixed);
        RenderOptions.SetBitmapScalingMode(canvas, BitmapScalingMode.HighQuality);
        canvas.UseLayoutRounding = true;
        canvas.SnapsToDevicePixels = true;

        canvas.Measure(new Size(docWidth, docHeight));
        canvas.Arrange(new Rect(0, 0, docWidth, docHeight));
        canvas.UpdateLayout();

        int effectiveDpi = dpi > 0 ? dpi : (int)PrintConstants.WpfDpi;
        double dpiScale = effectiveDpi / PrintConstants.WpfDpi;
        int bitmapWidth = (int)Math.Max(1, docWidth * dpiScale);
        int bitmapHeight = (int)Math.Max(1, docHeight * dpiScale);
        var renderBitmap = new RenderTargetBitmap(
            bitmapWidth, bitmapHeight, effectiveDpi, effectiveDpi, PixelFormats.Pbgra32);
        renderBitmap.Render(canvas);
        return renderBitmap;
    }

    /// <summary>
    /// 在可印区四角绘制 L 形套准（registration）标志，便于后道工序对齐。
    /// 标志长度 5mm、线宽 0.3mm；坐标基于真实可印区原点和宽高。
    /// </summary>
    private static void AddRegistrationMarks(FixedPage fixedPage, double originX, double originY, double extentW, double extentH)
    {
        var mmToDip = DesignConstants.MmToPixel;
        double markLen = 5.0 * mmToDip;
        double thickness = 0.3 * mmToDip;

        double x0 = originX;
        double y0 = originY;
        double x1 = originX + extentW;
        double y1 = originY + extentH;

        var corners = new (double x, double y, double dx, double dy)[]
        {
            (x0, y0,  1,  0), (x0, y0,  0,  1), // 左上
            (x1, y0, -1,  0), (x1, y0,  0,  1), // 右上
            (x0, y1,  1,  0), (x0, y1,  0, -1), // 左下
            (x1, y1, -1,  0), (x1, y1,  0, -1), // 右下
        };

        foreach (var (x, y, dx, dy) in corners)
        {
            fixedPage.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = x,
                Y1 = y,
                X2 = x + dx * markLen,
                Y2 = y + dy * markLen,
                Stroke = Brushes.Black,
                StrokeThickness = thickness,
                SnapsToDevicePixels = true
            });
        }
    }
}
