namespace SecureFix.Core.Services;

using Microsoft.EntityFrameworkCore;
using SecureFix.Core.Data;
using SecureFix.Core.Models;

public interface IDashboardQueryService
{
    Task<WorkflowListResponseDto> GetWorkflowsAsync(
        WorkflowListQueryDto query,
        CancellationToken cancellationToken = default);

    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public sealed class DashboardQueryService : IDashboardQueryService
{
    private const int MaximumPageSize = 100;
    private const int MaximumSearchLength = 100;
    private readonly SecureFixDbContext _dbContext;

    public DashboardQueryService(SecureFixDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<WorkflowListResponseDto> GetWorkflowsAsync(
        WorkflowListQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        var search = NormalizeSearch(query.Search);
        var severity = ParseOptionalEnum<Severity>(query.Severity, nameof(query.Severity));
        var status = ParseOptionalEnum<WorkflowStatus>(query.Status, nameof(query.Status));
        var sortBy = NormalizeSortBy(query.SortBy);
        var descending = NormalizeSortDirection(query.SortDirection);

        var workflows = CreateWorkflowQuery();

        if (search is not null)
        {
            var pattern = $"%{EscapeLikePattern(search.ToLowerInvariant())}%";
            workflows = workflows.Where(workflow =>
                EF.Functions.Like(workflow.WorkflowId.ToLower(), pattern, "\\") ||
                EF.Functions.Like(workflow.ExternalAlertId.ToLower(), pattern, "\\") ||
                EF.Functions.Like(workflow.PackageName.ToLower(), pattern, "\\") ||
                (workflow.CveId != null && EF.Functions.Like(workflow.CveId.ToLower(), pattern, "\\")) ||
                (workflow.RepositoryIdentifier != null &&
                    EF.Functions.Like(workflow.RepositoryIdentifier.ToLower(), pattern, "\\")));
        }

        if (severity.HasValue)
        {
            workflows = workflows.Where(workflow => workflow.Severity == severity);
        }

        if (status.HasValue)
        {
            workflows = workflows.Where(workflow => workflow.Status == status);
        }

        var totalCount = await workflows.CountAsync(cancellationToken);
        var orderedWorkflows = ApplySorting(workflows, sortBy, descending);
        var items = await orderedWorkflows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(workflow => new WorkflowListItemDto
            {
                WorkflowId = workflow.WorkflowId,
                CorrelationId = workflow.CorrelationId,
                ExternalAlertId = workflow.ExternalAlertId,
                CveId = workflow.CveId,
                PackageName = workflow.PackageName,
                InstalledVersion = workflow.InstalledVersion,
                FixedVersion = workflow.FixedVersion,
                RepositoryIdentifier = workflow.RepositoryIdentifier,
                Severity = workflow.Severity,
                RiskScore = workflow.RiskScore,
                Status = workflow.Status,
                RecommendedAction = workflow.RecommendedAction,
                Reviewer = workflow.Reviewer,
                ReceivedAt = workflow.ReceivedAt,
                UpdatedAt = workflow.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new WorkflowListResponseDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var aggregate = await CreateWorkflowQuery()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Pending = group.Count(workflow => workflow.Status == WorkflowStatus.PendingApproval),
                Approved = group.Count(workflow => workflow.Status == WorkflowStatus.Approved),
                Rejected = group.Count(workflow => workflow.Status == WorkflowStatus.Rejected),
                Critical = group.Count(workflow => workflow.Severity == Severity.Critical),
                High = group.Count(workflow => workflow.Severity == Severity.High),
                Medium = group.Count(workflow => workflow.Severity == Severity.Medium),
                Low = group.Count(workflow => workflow.Severity == Severity.Low),
                WithRecommendations = group.Count(workflow => workflow.RecommendedAction != null),
                AverageRiskScore = group
                    .Where(workflow => workflow.RiskScore != null)
                    .Average(workflow => (double?)workflow.RiskScore) ?? 0
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            TotalWorkflows = aggregate?.Total ?? 0,
            PendingApprovalWorkflows = aggregate?.Pending ?? 0,
            ApprovedWorkflows = aggregate?.Approved ?? 0,
            RejectedWorkflows = aggregate?.Rejected ?? 0,
            CriticalWorkflows = aggregate?.Critical ?? 0,
            HighWorkflows = aggregate?.High ?? 0,
            MediumWorkflows = aggregate?.Medium ?? 0,
            LowWorkflows = aggregate?.Low ?? 0,
            WorkflowsWithRecommendations = aggregate?.WithRecommendations ?? 0,
            AverageRiskScore = aggregate?.AverageRiskScore ?? 0,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private IQueryable<WorkflowQueryRow> CreateWorkflowQuery()
    {
        return _dbContext.VulnerabilityAlerts
            .AsNoTracking()
            .Select(alert => new WorkflowQueryRow
            {
                WorkflowId = alert.Id,
                CorrelationId = alert.CorrelationId,
                ExternalAlertId = alert.ExternalAlertId,
                CveId = alert.CveId,
                PackageName = alert.PackageName,
                InstalledVersion = alert.InstalledVersion,
                FixedVersion = alert.FixedVersion,
                RepositoryIdentifier = alert.RepositoryIdentifier,
                Severity = _dbContext.RiskAssessments
                    .AsNoTracking()
                    .Where(assessment => assessment.AlertId == alert.Id)
                    .OrderByDescending(assessment => assessment.AssessedAt.ToString())
                    .ThenByDescending(assessment => assessment.Id)
                    .Select(assessment => (Severity?)assessment.NormalizedSeverity)
                    .FirstOrDefault(),
                RiskScore = _dbContext.RiskAssessments
                    .AsNoTracking()
                    .Where(assessment => assessment.AlertId == alert.Id)
                    .OrderByDescending(assessment => assessment.AssessedAt.ToString())
                    .ThenByDescending(assessment => assessment.Id)
                    .Select(assessment => (int?)assessment.RiskScore)
                    .FirstOrDefault(),
                DecisionStatus = _dbContext.ApprovalDecisions
                    .AsNoTracking()
                    .Where(decision => decision.AlertId == alert.Id)
                    .OrderByDescending(decision => decision.DecisionTime.ToString())
                    .ThenByDescending(decision => decision.Id)
                    .Select(decision => (ApprovalStatus?)decision.Status)
                    .FirstOrDefault(),
                Reviewer = _dbContext.ApprovalDecisions
                    .AsNoTracking()
                    .Where(decision => decision.AlertId == alert.Id)
                    .OrderByDescending(decision => decision.DecisionTime.ToString())
                    .ThenByDescending(decision => decision.Id)
                    .Select(decision => decision.ReviewerIdentity)
                    .FirstOrDefault(),
                RecommendedAction = _dbContext.RemediationRecommendations
                    .AsNoTracking()
                    .Where(recommendation => recommendation.AlertId == alert.Id)
                    .OrderByDescending(recommendation => recommendation.GeneratedAt.ToString())
                    .ThenByDescending(recommendation => recommendation.Id)
                    .Select(recommendation => recommendation.RecommendedAction)
                    .FirstOrDefault(),
                RecommendationGeneratedAt = _dbContext.RemediationRecommendations
                    .AsNoTracking()
                    .Where(recommendation => recommendation.AlertId == alert.Id)
                    .OrderByDescending(recommendation => recommendation.GeneratedAt.ToString())
                    .ThenByDescending(recommendation => recommendation.Id)
                    .Select(recommendation => (DateTimeOffset?)recommendation.GeneratedAt)
                    .FirstOrDefault(),
                AssessmentAt = _dbContext.RiskAssessments
                    .AsNoTracking()
                    .Where(assessment => assessment.AlertId == alert.Id)
                    .OrderByDescending(assessment => assessment.AssessedAt.ToString())
                    .ThenByDescending(assessment => assessment.Id)
                    .Select(assessment => (DateTimeOffset?)assessment.AssessedAt)
                    .FirstOrDefault(),
                DecisionAt = _dbContext.ApprovalDecisions
                    .AsNoTracking()
                    .Where(decision => decision.AlertId == alert.Id)
                    .OrderByDescending(decision => decision.DecisionTime.ToString())
                    .ThenByDescending(decision => decision.Id)
                    .Select(decision => (DateTimeOffset?)decision.DecisionTime)
                    .FirstOrDefault(),
                ReceivedAt = alert.ReceivedAt
            })
            .Select(workflow => new WorkflowQueryRow
            {
                WorkflowId = workflow.WorkflowId,
                CorrelationId = workflow.CorrelationId,
                ExternalAlertId = workflow.ExternalAlertId,
                CveId = workflow.CveId,
                PackageName = workflow.PackageName,
                InstalledVersion = workflow.InstalledVersion,
                FixedVersion = workflow.FixedVersion,
                RepositoryIdentifier = workflow.RepositoryIdentifier,
                Severity = workflow.Severity,
                RiskScore = workflow.RiskScore,
                DecisionStatus = workflow.DecisionStatus,
                Status = workflow.DecisionStatus == ApprovalStatus.Approved
                    ? WorkflowStatus.Approved
                    : workflow.DecisionStatus == ApprovalStatus.Rejected
                        ? WorkflowStatus.Rejected
                        : workflow.RiskScore != null
                            ? WorkflowStatus.PendingApproval
                            : WorkflowStatus.Received,
                RecommendedAction = workflow.RecommendedAction,
                Reviewer = workflow.Reviewer,
                ReceivedAt = workflow.ReceivedAt,
                UpdatedAt = workflow.DecisionAt ??
                    workflow.RecommendationGeneratedAt ??
                    workflow.AssessmentAt ??
                    workflow.ReceivedAt
            });
    }

    private static IOrderedQueryable<WorkflowQueryRow> ApplySorting(
        IQueryable<WorkflowQueryRow> workflows,
        string sortBy,
        bool descending)
    {
        return sortBy switch
        {
            "severity" => descending
                ? workflows.OrderByDescending(workflow => workflow.Severity)
                    .ThenByDescending(workflow => workflow.ReceivedAt.ToString())
                    .ThenBy(workflow => workflow.WorkflowId)
                : workflows.OrderBy(workflow => workflow.Severity)
                    .ThenByDescending(workflow => workflow.ReceivedAt.ToString())
                    .ThenBy(workflow => workflow.WorkflowId),
            "riskscore" => descending
                ? workflows.OrderByDescending(workflow => workflow.RiskScore)
                    .ThenByDescending(workflow => workflow.ReceivedAt.ToString())
                    .ThenBy(workflow => workflow.WorkflowId)
                : workflows.OrderBy(workflow => workflow.RiskScore)
                    .ThenByDescending(workflow => workflow.ReceivedAt.ToString())
                    .ThenBy(workflow => workflow.WorkflowId),
            "status" => descending
                ? workflows.OrderByDescending(workflow => workflow.Status)
                    .ThenByDescending(workflow => workflow.ReceivedAt.ToString())
                    .ThenBy(workflow => workflow.WorkflowId)
                : workflows.OrderBy(workflow => workflow.Status)
                    .ThenByDescending(workflow => workflow.ReceivedAt.ToString())
                    .ThenBy(workflow => workflow.WorkflowId),
            "packagename" => descending
                ? workflows.OrderByDescending(workflow => workflow.PackageName)
                    .ThenBy(workflow => workflow.WorkflowId)
                : workflows.OrderBy(workflow => workflow.PackageName)
                    .ThenBy(workflow => workflow.WorkflowId),
            _ => descending
                ? workflows.OrderByDescending(workflow => workflow.ReceivedAt.ToString())
                    .ThenBy(workflow => workflow.WorkflowId)
                : workflows.OrderBy(workflow => workflow.ReceivedAt.ToString())
                    .ThenBy(workflow => workflow.WorkflowId)
        };
    }

    private static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var normalized = search.Trim();
        if (normalized.Length > MaximumSearchLength)
        {
            throw new ArgumentException(
                $"Search cannot exceed {MaximumSearchLength} characters.",
                nameof(search));
        }

        return normalized;
    }

    private static TEnum? ParseOptionalEnum<TEnum>(string? value, string parameterName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<TEnum>(value.Trim(), true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new ArgumentException(
                $"'{value}' is not a valid {typeof(TEnum).Name}.",
                parameterName);
        }

        return parsed;
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        var normalized = string.IsNullOrWhiteSpace(sortBy)
            ? "receivedat"
            : sortBy.Trim().ToLowerInvariant();

        return normalized is "receivedat" or "severity" or "riskscore" or "status" or "packagename"
            ? normalized
            : throw new ArgumentException(
                "SortBy must be one of: receivedAt, severity, riskScore, status, packageName.",
                nameof(sortBy));
    }

    private static bool NormalizeSortDirection(string? sortDirection)
    {
        var normalized = string.IsNullOrWhiteSpace(sortDirection)
            ? "desc"
            : sortDirection.Trim().ToLowerInvariant();

        return normalized switch
        {
            "asc" => false,
            "desc" => true,
            _ => throw new ArgumentException(
                "SortDirection must be either 'asc' or 'desc'.",
                nameof(sortDirection))
        };
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private sealed class WorkflowQueryRow
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
        public ApprovalStatus? DecisionStatus { get; set; }
        public WorkflowStatus Status { get; set; }
        public string? RecommendedAction { get; set; }
        public string? Reviewer { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
        public DateTimeOffset? AssessmentAt { get; set; }
        public DateTimeOffset? RecommendationGeneratedAt { get; set; }
        public DateTimeOffset? DecisionAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
