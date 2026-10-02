namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for approval decisions.
/// </summary>
public class ApprovalRequestDto
{
    /// <summary>
    /// Legacy client field; ignored. Reviewer identity comes from authenticated claims.
    /// </summary>
    [StringLength(255)]
    public string? Reviewer { get; set; }

    /// <summary>
    /// Legacy client field; ignored. Reviewer role comes from authenticated claims.
    /// </summary>
    [StringLength(100)]
    public string? ReviewerRole { get; set; }

    /// <summary>
    /// Approval decision: "approved" or "rejected".
    /// </summary>
    [Required]
    [RegularExpression(@"^(approved|rejected)$", ErrorMessage = "Decision must be 'approved' or 'rejected'")]
    public string Decision { get; set; } = null!;

    /// <summary>
    /// Reason for decision (optional for approval, recommended for rejection).
    /// </summary>
    [StringLength(1000)]
    public string? Reason { get; set; }
}
