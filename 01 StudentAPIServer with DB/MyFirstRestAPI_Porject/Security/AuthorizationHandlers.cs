using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace StudentApi.Security;

public sealed class StudentOwnerOrAdminRequirement : IAuthorizationRequirement;

public sealed class StudentOwnerOrAdminHandler
    : AuthorizationHandler<StudentOwnerOrAdminRequirement, HttpContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StudentOwnerOrAdminRequirement requirement,
        HttpContext resource)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!resource.Request.RouteValues.TryGetValue("id", out var routeIdValue))
        {
            return Task.CompletedTask;
        }

        var studentIdClaim = context.User.FindFirst("student_id")?.Value;
        if (int.TryParse(routeIdValue?.ToString(), out var routeId)
            && int.TryParse(studentIdClaim, out var ownedStudentId)
            && routeId == ownedStudentId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
