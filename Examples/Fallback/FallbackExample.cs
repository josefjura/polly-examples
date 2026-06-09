using Polly;
using Polly.Fallback;
using Polly.Retry;
using PollyRetryExamples.Services;

namespace PollyRetryExamples.Examples.Fallback;

/// <summary>
/// Example 10: Fallback — provides an alternative result when the primary operation
/// fails, so callers always receive a usable value instead of an exception.
///
/// Two scenarios are shown:
///   A) Simple fallback — service is down, return a cached/default value immediately.
///   B) Retry + Fallback — retry up to N times first; only fall back if all retries fail.
///      The pipeline executes inner-to-outer: Retry runs first, Fallback catches whatever
///      the Retry layer couldn't handle.
///
/// Best for: graceful degradation — serving stale data, default content, or a friendly
/// error payload instead of propagating an exception to the caller.
/// </summary>
public static class FallbackExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 10: Fallback ===");

        await DemonstrateSimpleFallbackAsync();
        await DemonstrateRetryThenFallbackAsync();

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // Scenario A: immediate fallback — no retries, just return a safe default
    // -------------------------------------------------------------------------
    private static async Task DemonstrateSimpleFallbackAsync()
    {
        Console.WriteLine("\n  [A] Simple fallback — service always fails, return cached value");

        var service = new UnreliableService("AlwaysDown", failuresBeforeSuccess: 99);

        var pipeline = new ResiliencePipelineBuilder<string>()
            .AddFallback(new FallbackStrategyOptions<string>
            {
                ShouldHandle = new PredicateBuilder<string>().Handle<HttpRequestException>(),

                FallbackAction = args =>
                {
                    // Return a stale cached value, a default, or a user-friendly message.
                    const string cachedValue = "[cached] Last known good data";
                    return ValueTask.FromResult(Outcome.FromResult(cachedValue));
                },

                OnFallback = args =>
                {
                    Console.WriteLine($"    Falling back — original error: {args.Outcome.Exception?.Message}");
                    return default;
                }
            })
            .Build();

        var result = await pipeline.ExecuteAsync(async ct => await service.GetDataAsync(ct));
        Console.WriteLine($"    Caller received: \"{result}\"");
    }

    // -------------------------------------------------------------------------
    // Scenario B: retry first, then fall back only if all retries are exhausted
    // -------------------------------------------------------------------------
    private static async Task DemonstrateRetryThenFallbackAsync()
    {
        Console.WriteLine("\n  [B] Retry (×2) then fallback — service never recovers in this run");

        var service = new UnreliableService("StillDown", failuresBeforeSuccess: 99);

        // Pipeline execution order: Fallback (outer) → Retry (inner) → operation
        // The Retry fires first; if it exhausts its attempts, Fallback catches the final exception.
        var pipeline = new ResiliencePipelineBuilder<string>()
            .AddFallback(new FallbackStrategyOptions<string>
            {
                ShouldHandle = new PredicateBuilder<string>().Handle<HttpRequestException>(),

                FallbackAction = args =>
                {
                    const string degraded = "[degraded] Service unavailable — showing placeholder";
                    return ValueTask.FromResult(Outcome.FromResult(degraded));
                },

                OnFallback = args =>
                {
                    Console.WriteLine($"    All retries exhausted, activating fallback.");
                    return default;
                }
            })
            .AddRetry(new RetryStrategyOptions<string>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder<string>().Handle<HttpRequestException>(),

                OnRetry = args =>
                {
                    Console.WriteLine($"    Retry #{args.AttemptNumber}: {args.Outcome.Exception?.Message}");
                    return default;
                }
            })
            .Build();

        var result = await pipeline.ExecuteAsync(async ct => await service.GetDataAsync(ct));
        Console.WriteLine($"    Caller received: \"{result}\"");
    }
}
