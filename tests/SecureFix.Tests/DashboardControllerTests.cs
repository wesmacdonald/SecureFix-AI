namespace SecureFix.Tests;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SecureFix.Api.Controllers;
using SecureFix.Core.Models;
using SecureFix.Core.Services;

public sealed class DashboardControllerTests
{
    [Fact]
    public async Task GetWorkflows_ReturnsQueryServiceResponse()
    {
        var expected = new WorkflowListResponseDto
        {
            Page = 2,
            PageSize = 10,
            TotalCount = 15,
            TotalPages = 2
        };
        var queryService = new Mock<IDashboardQueryService>();
        queryService
            .Setup(service => service.GetWorkflowsAsync(
                It.IsAny<WorkflowListQueryDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new WorkflowsController(
            Mock.Of<IApprovalService>(),
            queryService.Object,
            Mock.Of<ILogger<WorkflowsController>>());

        var result = await controller.GetWorkflows(new WorkflowListQueryDto
        {
            Page = 2,
            PageSize = 10
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetWorkflows_InvalidQuery_ReturnsValidationProblem()
    {
        var queryService = new Mock<IDashboardQueryService>();
        queryService
            .Setup(service => service.GetWorkflowsAsync(
                It.IsAny<WorkflowListQueryDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Unsupported sort.", "sortBy"));
        var controller = new WorkflowsController(
            Mock.Of<IApprovalService>(),
            queryService.Object,
            Mock.Of<ILogger<WorkflowsController>>());

        var result = await controller.GetWorkflows(
            new WorkflowListQueryDto { SortBy = "unsupported" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var details = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(400, details.Status);
    }

    [Fact]
    public async Task GetSummary_ReturnsQueryServiceResponse()
    {
        var expected = new DashboardSummaryDto { TotalWorkflows = 7 };
        var queryService = new Mock<IDashboardQueryService>();
        queryService
            .Setup(service => service.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new DashboardController(queryService.Object);

        var result = await controller.GetSummary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Theory]
    [InlineData(typeof(WorkflowsController), nameof(WorkflowsController.GetWorkflows))]
    [InlineData(typeof(DashboardController), nameof(DashboardController.GetSummary))]
    public void ReadEndpoints_AllowAllExistingReadRoles(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName);
        var authorize = Assert.Single(method!.GetCustomAttributes(typeof(AuthorizeAttribute), true))
            as AuthorizeAttribute;

        Assert.Equal("Viewer,Developer,SecurityReviewer,Admin", authorize!.Roles);
    }
}
