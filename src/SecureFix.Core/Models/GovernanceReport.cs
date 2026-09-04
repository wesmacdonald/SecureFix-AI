namespace SecureFix.Core.Models;

public sealed class GovernanceReport
{
    public required string AlertId { get; init; }
    public required string CorrelationId { get; init; }
    public required int Severity { get; init; }
    public required string RequiredApprovalLevel { get; init; }
    public required string ApprovalStatus { get; init; }
    public string? Reviewer { get; init; }
    public DateTimeOffset? ApprovalTimestamp { get; init; }
    public string? RecommendedAction { get; init; }
    public decimal? RecommendationConfidence { get; init; }
    public string? ModelName { get; init; }
    public string? PromptVersion { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
}
