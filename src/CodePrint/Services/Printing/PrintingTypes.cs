using CodePrint.Models;

namespace CodePrint.Services.Printing;

/// <summary>
/// 打印机基本信息。来源可能是本地 spooler、网络发现（mDNS/LPD）或人工配置。
/// 这是一个 DTO：不依赖 System.Printing，方便单元测试和未来跨实现。
/// </summary>
public sealed record PrinterInfo(
    string Name,
    bool IsDefault = false,
    bool IsAvailable = true);

/// <summary>
/// 单个打印作业的输入。把"打印什么"与"怎么打印"打包给 <see cref="IPrintService"/>，
/// ViewModel 不再直接构造 PrintTicket / FixedDocument。
/// </summary>
public sealed record PrintJobRequest(
    LabelDocument Document,
    PrintSettings Settings,
    string PrinterName,
    string JobName);

/// <summary>
/// 作业生命周期阶段。用于 UI 显示进度条/状态文字，以及为后续日志/遥测打点。
/// 顺序：Preparing → Rendering → Spooling → Submitted（成功终态）。
/// 失败/取消是另两条终态分支。
/// </summary>
public enum PrintJobStage
{
    Preparing,
    Rendering,
    Spooling,
    Submitted,
    Failed,
    Cancelled
}

/// <summary>
/// 进度回调载荷。<paramref name="Fraction"/> 取值 [0,1]，未知时传 -1。
/// </summary>
public sealed record PrintProgress(
    PrintJobStage Stage,
    string Message,
    double Fraction = -1);

/// <summary>打印作业终态结果。</summary>
public sealed record PrintJobResult(
    bool Success,
    string? JobId = null,
    string? ErrorMessage = null)
{
    public static PrintJobResult Ok(string? jobId = null) => new(true, jobId);
    public static PrintJobResult Fail(string message) => new(false, null, message);
    public static PrintJobResult Cancel() => new(false, null, "已取消");
}
