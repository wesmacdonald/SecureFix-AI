namespace SecureFix.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFix.Core.Models;
using SecureFix.Core.Services;

[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardQueryService _dashboardQueryService;

    public DashboardController(IDashboardQueryService dashboardQueryService)
    {
        _dashboardQueryService = dashboardQueryService ??
            throw new ArgumentNullException(nameof(dashboardQueryService));
    }

    [Authorize(Roles = "Viewer,Developer,SecurityReviewer,Admin")]
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _dashboardQueryService.GetSummaryAsync(cancellationToken));
    }
}
