namespace SecureFix.Tests;

using SecureFix.Core.Services;

public class OperationalMetricsTests
{
    [Fact]
    public void GetSnapshot_RecordsAllRequiredOperationalSignals()
    {
        var metrics = new OperationalMetrics();

        metrics.RecordRequest(failed: false);
        metrics.RecordRequest(failed: true);
        metrics.RecordVulnerabilityAnalyzed();
        metrics.RecordAICall(failed: false);
        metrics.RecordAICall(failed: true);
        metrics.RecordApproval();
        metrics.RecordRejection();

        var snapshot = metrics.GetSnapshot();

        Assert.Equal(2, snapshot.RequestsProcessed);
        Assert.Equal(1, snapshot.FailedRequests);
        Assert.Equal(1, snapshot.VulnerabilitiesAnalyzed);
        Assert.Equal(2, snapshot.AICalls);
        Assert.Equal(1, snapshot.FailedAICalls);
        Assert.Equal(1, snapshot.Approvals);
        Assert.Equal(1, snapshot.Rejections);
    }
}
