namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

public sealed class WorkflowListQueryDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(100)]
    public string? Search { get; set; }

    [StringLength(20)]
    public string? Severity { get; set; }

    [StringLength(30)]
    public string? Status { get; set; }

    [StringLength(30)]
    public string SortBy { get; set; } = "receivedAt";

    [StringLength(4)]
    public string SortDirection { get; set; } = "desc";
}

public sealed class WorkflowListResponseDto
{
    public IReadOnlyList<WorkflowListItemDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public sealed class WorkflowListItemDto
{
    public string WorkflowId { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public string ExternalAlertId { get; set; } = null!;
    public string? CveId { get; set; }
    public string PackageName { get; set; } = null!;
    public string InstalledVersion { get; set; } = null!;
    public string? FixedVersion { get; set; }
    public string? RepositoryIdentifier { get; set; }
    public Severity? Severity { get; set; }
    public int? RiskScore { get; set; }
    public WorkflowStatus Status { get; set; }
    public string? RecommendedAction { get; set; }
    public string? Reviewer { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class DashboardSummaryDto
{
    public int TotalWorkflows { get; set; }
    public int PendingApprovalWorkflows { get; set; }
    public int ApprovedWorkflows { get; set; }
    public int RejectedWorkflows { get; set; }
    public int CriticalWorkflows { get; set; }
    public int HighWorkflows { get; set; }
    public int MediumWorkflows { get; set; }
    public int LowWorkflows { get; set; }
    public int WorkflowsWithRecommendations { get; set; }
    public double AverageRiskScore { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}
