using AuthService.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Authorization;

public sealed class DbPermissionPolicyProvider(
    IOptions<AuthorizationOptions> options,
    IServiceScopeFactory scopeFactory) : DefaultAuthorizationPolicyProvider(options)
{
    private readonly AuthorizationOptions _options = options.Value;
    private readonly Dictionary<string, AuthorizationPolicy> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cacheLock = new();

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(policyName, out var cachedPolicy))
            {
                return cachedPolicy;
            }
        }

        var dbPolicy = await CreatePolicyFromDatabaseAsync(policyName);
        if (dbPolicy is not null)
        {
            lock (_cacheLock)
            {
                _cache[policyName] = dbPolicy;
            }

            return dbPolicy;
        }

        return await base.GetPolicyAsync(policyName);
    }

    private async Task<AuthorizationPolicy?> CreatePolicyFromDatabaseAsync(string policyName)
    {
        using var scope = scopeFactory.CreateScope();
        var permissionRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();

        if (!await permissionRepository.ExistsByCodeAsync(policyName))
        {
            return null;
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new ClaimsAuthorizationRequirement(Shared.Constants.ClaimConstants.Permission, [policyName]))
            .Build();
    }
}