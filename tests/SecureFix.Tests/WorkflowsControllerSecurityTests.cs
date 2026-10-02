namespace SecureFix.Tests;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SecureFix.Api.Controllers;
using SecureFix.Core.Models;
using SecureFix.Core.Services;

public class WorkflowsControllerSecurityTests
{
    [Fact]
    public async Task ApproveWorkflow_UsesAuthenticatedReviewerInsteadOfRequestIdentity()
    {
        var response = new WorkflowStatusResponse();
        var approvalService = new Mock<IApprovalService>();
        approvalService
            .Setup(service => service.ApproveAlertAsync(
                "workflow-1",
                "authenticated-reviewer",
                "reviewed",
                "SecurityReviewer"))
            .ReturnsAsync(response);
        var controller = CreateController(approvalService, "authenticated-reviewer", "SecurityReviewer");

        var result = await controller.ApproveWorkflow("workflow-1", new ApprovalRequestDto
        {
            Decision = "approved",
            Reviewer = "forged-reviewer",
            ReviewerRole = "Admin",
            Reason = "reviewed"
        });

        Assert.Same(response, Assert.IsType<OkObjectResult>(result).Value);
        approvalService.VerifyAll();
    }

    [Fact]
    public async Task RejectWorkflow_UsesAuthenticatedReviewerInsteadOfRequestIdentity()
    {
        var response = new WorkflowStatusResponse();
        var approvalService = new Mock<IApprovalService>();
        approvalService
            .Setup(service => service.RejectAlertAsync(
                "workflow-1",
                "authenticated-reviewer",
                "needs more testing",
                "SecurityReviewer"))
            .ReturnsAsync(response);
        var controller = CreateController(approvalService, "authenticated-reviewer", "SecurityReviewer");

        var result = await controller.RejectWorkflow("workflow-1", new ApprovalRequestDto
        {
            Decision = "rejected",
            Reviewer = "forged-reviewer",
            ReviewerRole = "Admin",
            Reason = "needs more testing"
        });

        Assert.Same(response, Assert.IsType<OkObjectResult>(result).Value);
        approvalService.VerifyAll();
    }

    [Fact]
    public async Task ApproveWorkflow_RejectsCallerWithoutAuthenticatedReviewerIdentity()
    {
        var approvalService = new Mock<IApprovalService>();
        var controller = CreateController(approvalService, null, "SecurityReviewer");

        var result = await controller.ApproveWorkflow("workflow-1", new ApprovalRequestDto
        {
            Decision = "approved",
            Reviewer = "forged-reviewer",
            ReviewerRole = "SecurityReviewer"
        });

        Assert.IsType<ForbidResult>(result);
        approvalService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApproveWorkflow_RejectsDeveloperDespiteReviewerRoleInRequest()
    {
        var approvalService = new Mock<IApprovalService>();
        var controller = CreateController(approvalService, "authenticated-developer", "Developer");

        var result = await controller.ApproveWorkflow("workflow-1", new ApprovalRequestDto
        {
            Decision = "approved",
            Reviewer = "authenticated-developer",
            ReviewerRole = "SecurityReviewer"
        });

        Assert.IsType<ForbidResult>(result);
        approvalService.VerifyNoOtherCalls();
    }

    private static WorkflowsController CreateController(
        Mock<IApprovalService> approvalService,
        string? reviewerIdentity,
        string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role)
        };
        if (reviewerIdentity is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, reviewerIdentity));
        }

        var controller = new WorkflowsController(
            approvalService.Object,
            NullLogger<WorkflowsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            }
        };

        return controller;
    }
}
