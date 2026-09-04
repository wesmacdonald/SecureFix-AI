namespace SecureFix.Core.Repositories;

using Microsoft.EntityFrameworkCore;
using SecureFix.Core.Data;
using SecureFix.Core.Entities;

/// <summary>
/// Repository implementation for VulnerabilityAlert entities.
/// </summary>
public class VulnerabilityAlertRepository : BaseRepository<VulnerabilityAlertEntity>, IVulnerabilityAlertRepository
{
    public VulnerabilityAlertRepository(SecureFixDbContext context) : base(context) { }

    public async Task<VulnerabilityAlertEntity?> GetByExternalIdAsync(string externalAlertId)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.ExternalAlertId == externalAlertId);
    }

    public async Task<IEnumerable<VulnerabilityAlertEntity>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _dbSet
            .Where(a => a.CorrelationId == correlationId)
            .ToListAsync();
    }
}

/// <summary>
/// Repository implementation for RiskAssessment entities.
/// </summary>
public class RiskAssessmentRepository : BaseRepository<RiskAssessmentEntity>, IRiskAssessmentRepository
{
    public RiskAssessmentRepository(SecureFixDbContext context) : base(context) { }

    public async Task<RiskAssessmentEntity?> GetByAlertIdAsync(string alertId)
    {
        return (await _dbSet
            .Where(r => r.AlertId == alertId)
            .ToListAsync())
            .OrderByDescending(r => r.AssessedAt)
            .FirstOrDefault();
    }

    public async Task<IEnumerable<RiskAssessmentEntity>> GetBySeverityAsync(int severity)
    {
        return await _dbSet
            .Where(r => r.NormalizedSeverity == severity)
            .ToListAsync();
    }

    public async Task<IEnumerable<RiskAssessmentEntity>> GetByRiskScoreRangeAsync(int minScore, int maxScore)
    {
        return await _dbSet
            .Where(r => r.RiskScore >= minScore && r.RiskScore <= maxScore)
            .OrderByDescending(r => r.RiskScore)
            .ToListAsync();
    }
}

/// <summary>
/// Repository implementation for RemediationRecommendation entities.
/// </summary>
public class RemediationRecommendationRepository : BaseRepository<RemediationRecommendationEntity>, IRemediationRecommendationRepository
{
    public RemediationRecommendationRepository(SecureFixDbContext context) : base(context) { }

    public async Task<RemediationRecommendationEntity?> GetByAlertIdAsync(string alertId)
    {
        return (await _dbSet
            .Where(r => r.AlertId == alertId)
            .ToListAsync())
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefault();
    }

    public async Task<IEnumerable<RemediationRecommendationEntity>> GetByProviderAsync(string modelIdentifier)
    {
        return (await _dbSet
            .Where(r => r.ModelIdentifier == modelIdentifier)
            .ToListAsync())
            .OrderByDescending(r => r.GeneratedAt)
            .ToList();
    }

    public async Task<IEnumerable<RemediationRecommendationEntity>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _dbSet
            .Where(r => r.CorrelationId == correlationId)
            .ToListAsync();
    }
}

/// <summary>
/// Repository implementation for ApprovalDecision entities.
/// </summary>
public class ApprovalDecisionRepository : BaseRepository<ApprovalDecisionEntity>, IApprovalDecisionRepository
{
    public ApprovalDecisionRepository(SecureFixDbContext context) : base(context) { }

    public async Task<ApprovalDecisionEntity?> GetByWorkflowIdAsync(string workflowId)
    {
        return (await _dbSet
            .Where(a => a.WorkflowId == workflowId)
            .ToListAsync())
            .OrderByDescending(a => a.DecisionTime)
            .FirstOrDefault();
    }

    public async Task<ApprovalDecisionEntity?> GetByAlertIdAsync(string alertId)
    {
        return (await _dbSet
            .Where(a => a.AlertId == alertId)
            .ToListAsync())
            .OrderByDescending(a => a.DecisionTime)
            .FirstOrDefault();
    }

    public async Task<IEnumerable<ApprovalDecisionEntity>> GetByReviewerAsync(string reviewerIdentity)
    {
        return (await _dbSet
            .Where(a => a.ReviewerIdentity == reviewerIdentity)
            .ToListAsync())
            .OrderByDescending(a => a.DecisionTime)
            .ToList();
    }

    public async Task<IEnumerable<ApprovalDecisionEntity>> GetByStatusAsync(int status)
    {
        return (await _dbSet
            .Where(a => a.Status == status)
            .ToListAsync())
            .OrderByDescending(a => a.DecisionTime)
            .ToList();
    }
}

/// <summary>
/// Repository implementation for AuditEvent entities.
/// </summary>
public class AuditEventRepository : BaseRepository<AuditEventEntity>, IAuditEventRepository
{
    public AuditEventRepository(SecureFixDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditEventEntity>> GetByCorrelationIdAsync(string correlationId)
    {
        return (await _dbSet
            .Where(e => e.CorrelationId == correlationId)
            .ToListAsync())
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    public async Task<IEnumerable<AuditEventEntity>> GetByEventTypeAsync(string eventType)
    {
        return (await _dbSet
            .Where(e => e.EventType == eventType)
            .ToListAsync())
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }

    public async Task<IEnumerable<AuditEventEntity>> GetSecurityEventsAsync()
    {
        return (await _dbSet
            .Where(e => e.IsSecurityRelevant)
            .ToListAsync())
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }

    public async Task<IEnumerable<AuditEventEntity>> GetTimelineAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return (await _dbSet.ToListAsync())
            .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }
}

/// <summary>
/// Repository implementation for PullRequestProposal entities.
/// </summary>
public class PullRequestProposalRepository : BaseRepository<PullRequestProposalEntity>, IPullRequestProposalRepository
{
    public PullRequestProposalRepository(SecureFixDbContext context) : base(context) { }

    public async Task<PullRequestProposalEntity?> GetByRecommendationIdAsync(string recommendationId)
    {
        return (await _dbSet
            .Where(p => p.RecommendationId == recommendationId)
            .ToListAsync())
            .OrderByDescending(p => p.GeneratedAt)
            .FirstOrDefault();
    }

    public async Task<IEnumerable<PullRequestProposalEntity>> GetByAlertIdAsync(string alertId)
    {
        return (await _dbSet
            .Where(p => p.AlertId == alertId)
            .ToListAsync())
            .OrderByDescending(p => p.GeneratedAt)
            .ToList();
    }

    public async Task<IEnumerable<PullRequestProposalEntity>> GetByCorrelationIdAsync(string correlationId)
    {
        return (await _dbSet
            .Where(p => p.CorrelationId == correlationId)
            .ToListAsync())
            .OrderByDescending(p => p.GeneratedAt)
            .ToList();
    }
}
