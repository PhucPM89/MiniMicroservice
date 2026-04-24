using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Constants;

namespace AuthService.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class ResourcePermissionAuthorizeAttribute(string resource) : Attribute, IAsyncAuthorizationFilter
{
    private static readonly IReadOnlyDictionary<string, string> HttpMethodToAction =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [HttpMethods.Get] = "view",
            [HttpMethods.Post] = "create",
            [HttpMethods.Put] = "update",
            [HttpMethods.Patch] = "update",
            [HttpMethods.Delete] = "delete"
        };

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return Task.CompletedTask;
        }

        var httpMethod = context.HttpContext.Request.Method;
        if (!HttpMethodToAction.TryGetValue(httpMethod, out var action))
        {
            context.Result = new ForbidResult();
            return Task.CompletedTask;
        }

        var requiredPermission = $"{resource}.{action}";
        var hasPermission = user.Claims.Any(claim =>
            string.Equals(claim.Type, ClaimConstants.Permission, StringComparison.OrdinalIgnoreCase)
            && string.Equals(claim.Value, requiredPermission, StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }

        return Task.CompletedTask;
    }
}