using Polly;
using Polly.RateLimiting;
using System.Threading.RateLimiting;

namespace PollyRetryExamples.Examples.RateLimiter;

/// <summary>
/// Example 9: Rate Limiter — caps how many calls can be made within a time window,
/// protecting both the caller (from runaway loops) and the downstream service.
///
/// Polly v8 wraps System.Threading.RateLimiting, so any built-in limiter works:
///   FixedWindowRateLimiter   — N permits per fixed window (resets on the clock)
///   SlidingWindowRateLimiter — N permits per rolling window (smoother)
///   TokenBucketRateLimiter   — tokens consumed per call, refilled at a steady rate
///   ConcurrencyLimiter       — limits concurrent in-flight calls (bulkhead-style)
///
/// Rejected calls throw RateLimiterRejectedException immediately — no waiting.
///
/// Best for: enforcing client-side call budgets, honouring upstream API quotas,
/// and preventing burst traffic from overwhelming a shared resource.
/// </summary>
public static class RateLimiterExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 9: Rate Limiter ===");

        // Token bucket: starts with 3 tokens, refills 3 tokens every 3 seconds.
        // Each call consumes 1 token. Calls with no tokens available are rejected.
        using var tokenBucket = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 3,
            ReplenishmentPeriod = TimeSpan.FromSeconds(3),
            TokensPerPeriod = 3,
            AutoReplenishment = true,
            QueueLimit = 0,  // reject immediately instead of queuing
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });

        var pipeline = new ResiliencePipelineBuilder()
            .AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args => tokenBucket.AcquireAsync(permitCount: 1, args.Context.CancellationToken),

                OnRejected = args =>
                {
                    var hint = args.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? $"{retryAfter.TotalMilliseconds:F0}ms"
                        : "unknown";
                    Console.WriteLine($"    Rate limited — retry after: {hint}");
                    return default;
                }
            })
            .Build();

        var callCount = 0;

        async Task MakeCallAsync(string label)
        {
            callCount++;
            var n = callCount;
            try
            {
                await pipeline.ExecuteAsync(async ct =>
                {
                    await Task.Delay(20, ct); // simulate work
                    Console.WriteLine($"    {label} call #{n}: succeeded");
                });
            }
            catch (RateLimiterRejectedException)
            {
                Console.WriteLine($"    {label} call #{n}: REJECTED by rate limiter");
            }
        }

        // Phase 1: fire 6 calls rapidly — first 3 consume all tokens, next 3 are rejected
        Console.WriteLine("\n  [Phase 1] Firing 6 rapid calls (token bucket has 3 tokens)...");
        for (int i = 1; i <= 6; i++)
            await MakeCallAsync("rapid");

        // Phase 2: wait for the bucket to replenish, then try again
        Console.WriteLine($"\n  [Phase 2] Waiting for token replenishment (3s)...");
        await Task.Delay(TimeSpan.FromSeconds(3.2));

        Console.WriteLine("  [Phase 3] Firing 3 calls after replenishment...");
        for (int i = 1; i <= 3; i++)
            await MakeCallAsync("post-refill");

        Console.WriteLine();
    }
}
