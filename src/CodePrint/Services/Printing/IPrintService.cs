using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CodePrint.Services.Printing;

/// <summary>
/// 打印服务抽象。把"打印机发现 / 能力查询 / 作业提交 / 进度回调"
/// 收敛到一个接口背后，使 ViewModel 不再直接依赖 <c>System.Printing</c> /
/// <c>PrintDialog</c>，便于：
/// <list type="bullet">
///   <item>单元测试时 mock 打印结果，不需要真实打印机；</item>
///   <item>未来加入 ZPL / TSPL / ESC-POS 等驱动只需新增实现，不改 UI；</item>
///   <item>把同步阻塞调用替换为可取消、可观测的异步作业。</item>
/// </list>
/// </summary>
public interface IPrintService
{
    /// <summary>
    /// 该实现是否能枚举打印机。GDI 实现为 true；
    /// 直连 TCP/USB 的 ZPL/TSPL 实现通常为 false（由用户配置目标地址）。
    /// </summary>
    bool SupportsPrinterDiscovery { get; }

    /// <summary>枚举可用打印机。失败时返回空列表，不抛异常。</summary>
    Task<IReadOnlyList<PrinterInfo>> DiscoverPrintersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交一个打印作业。实现必须：
    /// <list type="bullet">
    ///   <item>在每个阶段调用 <paramref name="progress"/>（可空）；</item>
    ///   <item>响应 <paramref name="cancellationToken"/>，被取消时返回 Stage=Cancelled 的结果而非抛 <see cref="OperationCanceledException"/>；</item>
    ///   <item>不向上抛打印异常，全部包成 <see cref="PrintJobResult.Fail"/>。</item>
    /// </list>
    /// 注意：WPF/GDI 实现需要 STA 线程，调用方应在 UI 线程上 await。
    /// </summary>
    Task<PrintJobResult> PrintAsync(
        PrintJobRequest request,
        IProgress<PrintProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
