namespace SecureFix.Tests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureFix.Core.Data;
using SecureFix.Core.Entities;
using SecureFix.Core.Models;
using SecureFix.Core.Services;

public sealed class DashboardQueryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SecureFixDbContext _context;
    private readonly DashboardQueryService _service;

    public DashboardQueryServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SecureFixDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SecureFixDbContext(options);
        _context.Database.EnsureCreated();
        _service = new DashboardQueryService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetWorkflowsAsync_AppliesCombinedFiltersAndUsesLatestRelatedRecords()
    {
        var receivedAt = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        AddAlert("workflow-1", "api-gateway", "CVE-2026-1001", "org/service-api", receivedAt);
        AddAssessment("workflow-1", Severity.Low, 20, receivedAt.AddMinutes(1), "assessment-old");
        AddAssessment("workflow-1", Severity.Critical, 95, receivedAt.AddMinutes(2), "assessment-new");
        AddDecision("workflow-1", ApprovalStatus.Approved, receivedAt.AddMinutes(3), "decision-old");
        AddDecision("workflow-1", ApprovalStatus.Rejected, receivedAt.AddMinutes(4), "decision-new");
        AddRecommendation("workflow-1", "Upgrade", receivedAt.AddMinutes(2), "recommendation-old");
        AddRecommendation("workflow-1", "Replace", receivedAt.AddMinutes(5), "recommendation-new");

        AddAlert("workflow-2", "other-package", "CVE-2026-1002", "org/other", receivedAt);
        AddAssessment("workflow-2", Severity.Critical, 99, receivedAt.AddMinutes(1), "assessment-2");
        AddDecision("workflow-2", ApprovalStatus.Rejected, receivedAt.AddMinutes(2), "decision-2");
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _service.GetWorkflowsAsync(new WorkflowListQueryDto
        {
            Search = "SERVICE-API",
            Severity = "critical",
            Status = "rejected"
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("workflow-1", item.WorkflowId);
        Assert.Equal(Severity.Critical, item.Severity);
        Assert.Equal(95, item.RiskScore);
        Assert.Equal(WorkflowStatus.Rejected, item.Status);
        Assert.Equal("Replace", item.RecommendedAction);
        Assert.Equal("reviewer@example.com", item.Reviewer);
        Assert.Equal(receivedAt.AddMinutes(4), item.UpdatedAt);
        Assert.Empty(_context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetWorkflowsAsync_PaginatesWithinBoundsAndSortsDeterministically()
    {
        var receivedAt = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        AddAlert("workflow-c", "shared-package", "CVE-2026-1003", null, receivedAt);
        AddAlert("workflow-a", "shared-package", "CVE-2026-1001", null, receivedAt);
        AddAlert("workflow-b", "shared-package", "CVE-2026-1002", null, receivedAt);
        await _context.SaveChangesAsync();

        var firstPage = await _service.GetWorkflowsAsync(new WorkflowListQueryDto
        {
            Page = 1,
            PageSize = 2,
            SortBy = "packageName",
            SortDirection = "asc"
        });
        var boundedPage = await _service.GetWorkflowsAsync(new WorkflowListQueryDto
        {
            Page = 0,
            PageSize = 500,
            SortBy = "packageName",
            SortDirection = "asc"
        });

        Assert.Equal(["workflow-a", "workflow-b"], firstPage.Items.Select(item => item.WorkflowId));
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(1, boundedPage.Page);
        Assert.Equal(100, boundedPage.PageSize);
        Assert.Equal(["workflow-a", "workflow-b", "workflow-c"], boundedPage.Items.Select(item => item.WorkflowId));
    }

    [Fact]
    public async Task GetWorkflowsAsync_InvalidFilterOrSort_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetWorkflowsAsync(new WorkflowListQueryDto { Severity = "urgent" }));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetWorkflowsAsync(new WorkflowListQueryDto { Status = "running" }));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetWorkflowsAsync(new WorkflowListQueryDto { SortBy = "reviewer" }));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetWorkflowsAsync(new WorkflowListQueryDto { Search = new string('x', 101) }));
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsWorkflowSeverityAndRecommendationAggregates()
    {
        var receivedAt = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

        AddAlert("received", "package-a", "CVE-2026-1001", null, receivedAt);

        AddAlert("pending", "package-b", "CVE-2026-1002", null, receivedAt.AddMinutes(1));
        AddAssessment("pending", Severity.High, 70, receivedAt.AddMinutes(2), "assessment-pending");
        AddRecommendation("pending", "Upgrade", receivedAt.AddMinutes(3), "recommendation-pending");

        AddAlert("approved", "package-c", "CVE-2026-1003", null, receivedAt.AddMinutes(2));
        AddAssessment("approved", Severity.Critical, 90, receivedAt.AddMinutes(3), "assessment-approved");
        AddDecision("approved", ApprovalStatus.Approved, receivedAt.AddMinutes(4), "decision-approved");

        AddAlert("rejected", "package-d", "CVE-2026-1004", null, receivedAt.AddMinutes(3));
        AddAssessment("rejected", Severity.Medium, 40, receivedAt.AddMinutes(4), "assessment-rejected");
        AddDecision("rejected", ApprovalStatus.Rejected, receivedAt.AddMinutes(5), "decision-rejected");

        await _context.SaveChangesAsync();

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(4, summary.TotalWorkflows);
        Assert.Equal(1, summary.PendingApprovalWorkflows);
        Assert.Equal(1, summary.ApprovedWorkflows);
        Assert.Equal(1, summary.RejectedWorkflows);
        Assert.Equal(1, summary.CriticalWorkflows);
        Assert.Equal(1, summary.HighWorkflows);
        Assert.Equal(1, summary.MediumWorkflows);
        Assert.Equal(0, summary.LowWorkflows);
        Assert.Equal(1, summary.WorkflowsWithRecommendations);
        Assert.Equal(66.67, summary.AverageRiskScore, 2);
    }

    [Fact]
    public async Task GetSummaryAsync_EmptyDatabase_ReturnsZeroes()
    {
        var summary = await _service.GetSummaryAsync();

        Assert.Equal(0, summary.TotalWorkflows);
        Assert.Equal(0, summary.AverageRiskScore);
    }

    private void AddAlert(
        string id,
        string packageName,
        string cveId,
        string? repository,
        DateTimeOffset receivedAt)
    {
        _context.VulnerabilityAlerts.Add(new VulnerabilityAlertEntity
        {
            Id = id,
            CorrelationId = $"correlation-{id}",
            ExternalAlertId = $"external-{id}",
            CveId = cveId,
            PackageName = packageName,
            InstalledVersion = "1.0.0",
            FixedVersion = "1.0.1",
            ProviderSeverity = "high",
            RepositoryIdentifier = repository,
            ReceivedAt = receivedAt
        });
    }

    private void AddAssessment(
        string alertId,
        Severity severity,
        int riskScore,
        DateTimeOffset assessedAt,
        string id)
    {
        _context.RiskAssessments.Add(new RiskAssessmentEntity
        {
            Id = id,
            AlertId = alertId,
            CorrelationId = $"correlation-{alertId}",
            NormalizedSeverity = (int)severity,
            RiskScore = riskScore,
            ConfidenceScore = 90,
            RequiredApprovalLevel = "SecurityReviewer",
            AssessedAt = assessedAt
        });
    }

    private void AddDecision(
        string alertId,
        ApprovalStatus status,
        DateTimeOffset decisionTime,
        string id)
    {
        _context.ApprovalDecisions.Add(new ApprovalDecisionEntity
        {
            Id = id,
            WorkflowId = alertId,
            AlertId = alertId,
            CorrelationId = $"correlation-{alertId}",
            Status = (int)status,
            ReviewerIdentity = "reviewer@example.com",
            ReviewerRole = "SecurityReviewer",
            DecisionTime = decisionTime
        });
    }

    private void AddRecommendation(
        string alertId,
        string action,
        DateTimeOffset generatedAt,
        string id)
    {
        _context.RemediationRecommendations.Add(new RemediationRecommendationEntity
        {
            Id = id,
            AlertId = alertId,
            RiskAssessmentId = $"assessment-{alertId}",
            CorrelationId = $"correlation-{alertId}",
            RecommendedAction = action,
            Explanation = "Test recommendation",
            ConfidenceScore = 90,
            ModelIdentifier = "test-model",
            GeneratedAt = generatedAt
        });
    }
}
