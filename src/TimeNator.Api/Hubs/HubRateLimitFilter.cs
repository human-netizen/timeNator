using System.Threading.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using TimeNator.Api.Controllers;

namespace TimeNator.Api.Hubs;

/// <summary>
/// Rate limits hub calls per user. The HTTP rate limiter only sees the connection being
/// opened; everything after that is frames on one WebSocket, so hub methods need their own
/// limit. A token bucket allows a short burst, then a steady trickle.
/// </summary>
public sealed class HubRateLimitFilter(IConfiguration configuration) : IHubFilter, IDisposable
{
    private readonly PartitionedRateLimiter<Guid> _limiter = PartitionedRateLimiter.Create<Guid, Guid>(
        userId => RateLimitPartition.GetTokenBucketLimiter(userId, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = configuration.GetValue("RateLimits:HubBurst", 20),
            TokensPerPeriod = 5,
            ReplenishmentPeriod = TimeSpan.FromSeconds(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext context,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        using var lease = _limiter.AttemptAcquire(context.Context.User!.GetUserId());
        if (!lease.IsAcquired)
            throw new HubException("Too many requests. Slow down.");
        return await next(context);
    }

    public void Dispose() => _limiter.Dispose();
}
