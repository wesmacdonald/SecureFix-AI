namespace SecureFix.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFix.Core.Models;
using SecureFix.Core.Repositories;

[ApiController]
[Authorize(Roles = "SecurityReviewer,Admin")]
[Route("api/v1/workflows/{id}")]
[Produces("application/json")]
public class GovernanceController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public GovernanceController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    [HttpGet("audit-events")]
    [ProducesResponseType(typeof(IEnumerable<AuditEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditEvents(string id)
    {
        var alert = await _unitOfWork.VulnerabilityAlerts.GetByIdAsync(id);
        if (alert is null)
        {
            return NotFound(new ProblemDetails { Title = "Workflow Not Found", Status = StatusCodes.Status404NotFound });
        }

        var events = await _unitOfWork.AuditEvents.GetByCorrelationIdAsync(alert.CorrelationId);
        return Ok(events.Select(auditEvent => new AuditEvent
        {
            Id = auditEvent.Id,
            CorrelationId = auditEvent.CorrelationId,
            WorkflowId = auditEvent.WorkflowId,
            EventType = auditEvent.EventType,
            Level = auditEvent.Level,
            Summary = auditEvent.Summary,
            Details = auditEvent.Details,
            Actor = auditEvent.Actor,
            Service = auditEvent.Service,
            AlertId = auditEvent.AlertId,
            Metadata = auditEvent.Metadata,
            Timestamp = auditEvent.Timestamp,
            IsSecurityRelevant = auditEvent.IsSecurityRelevant
        }));
    }

    [HttpGet("governance-report")]
    [ProducesResponseType(typeof(GovernanceReport), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGovernanceReport(string id)
    {
        var alert = await _unitOfWork.VulnerabilityAlerts.GetByIdAsync(id);
        var assessment = await _unitOfWork.RiskAssessments.GetByAlertIdAsync(id);
        if (alert is null || assessment is null)
        {
            return NotFound(new ProblemDetails { Title = "Workflow Not Found", Status = StatusCodes.Status404NotFound });
        }

        var approval = await _unitOfWork.ApprovalDecisions.GetByAlertIdAsync(id);
        var recommendation = await _unitOfWork.RemediationRecommendations.GetByAlertIdAsync(id);

        return Ok(new GovernanceReport
        {
            AlertId = id,
            CorrelationId = alert.CorrelationId,
            Severity = assessment.NormalizedSeverity,
            RequiredApprovalLevel = assessment.RequiredApprovalLevel,
            ApprovalStatus = approval?.Status == (int)ApprovalStatus.Approved ? "Approved"
                : approval?.Status == (int)ApprovalStatus.Rejected ? "Rejected"
                : "Pending",
            Reviewer = approval?.ReviewerIdentity,
            ApprovalTimestamp = approval?.DecisionTime,
            RecommendedAction = recommendation?.RecommendedAction,
            RecommendationConfidence = recommendation?.ConfidenceScore,
            ModelName = recommendation?.ModelIdentifier,
            PromptVersion = recommendation?.PromptVersion,
            GeneratedAt = DateTimeOffset.UtcNow
        });
    }
}
