namespace SecureFix.Core.Services;

using System.Threading;

public interface IOperationalMetrics
{
    void RecordRequest(bool failed);
    void RecordVulnerabilityAnalyzed();
    void RecordAICall(bool failed);
    void RecordApproval();
    void RecordRejection();
    OperationalMetricsSnapshot GetSnapshot();
}

public sealed record OperationalMetricsSnapshot(
    long RequestsProcessed,
    long FailedRequests,
    long VulnerabilitiesAnalyzed,
    long AICalls,
    long FailedAICalls,
    long Approvals,
    long Rejections);

public sealed class OperationalMetrics : IOperationalMetrics
{
    private long _requestsProcessed;
    private long _failedRequests;
    private long _vulnerabilitiesAnalyzed;
    private long _aiCalls;
    private long _failedAICalls;
    private long _approvals;
    private long _rejections;

    public void RecordRequest(bool failed)
    {
        Interlocked.Increment(ref _requestsProcessed);
        if (failed)
        {
            Interlocked.Increment(ref _failedRequests);
        }
    }

    public void RecordVulnerabilityAnalyzed() => Interlocked.Increment(ref _vulnerabilitiesAnalyzed);

    public void RecordAICall(bool failed)
    {
        Interlocked.Increment(ref _aiCalls);
        if (failed)
        {
            Interlocked.Increment(ref _failedAICalls);
        }
    }

    public void RecordApproval() => Interlocked.Increment(ref _approvals);

    public void RecordRejection() => Interlocked.Increment(ref _rejections);

    public OperationalMetricsSnapshot GetSnapshot() => new(
        Interlocked.Read(ref _requestsProcessed),
        Interlocked.Read(ref _failedRequests),
        Interlocked.Read(ref _vulnerabilitiesAnalyzed),
        Interlocked.Read(ref _aiCalls),
        Interlocked.Read(ref _failedAICalls),
        Interlocked.Read(ref _approvals),
        Interlocked.Read(ref _rejections));
}

internal sealed class NullOperationalMetrics : IOperationalMetrics
{
    public static readonly NullOperationalMetrics Instance = new();

    private NullOperationalMetrics()
    {
    }

    public void RecordRequest(bool failed)
    {
    }

    public void RecordVulnerabilityAnalyzed()
    {
    }

    public void RecordAICall(bool failed)
    {
    }

    public void RecordApproval()
    {
    }

    public void RecordRejection()
    {
    }

    public OperationalMetricsSnapshot GetSnapshot() => new(0, 0, 0, 0, 0, 0, 0);
}
