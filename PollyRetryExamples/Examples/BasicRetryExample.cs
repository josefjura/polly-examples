using Polly;
using Polly.Retry;
using PollyRetryExamples.Services;

namespace PollyRetryExamples.Examples;

/// <summary>
/// Example 1: Basic retry — retries immediately up to N times on any exception.
/// Best for: operations that may fail transiently and recover instantly
/// (e.g. in-memory cache misses, quick I/O glitches).
/// </summary>
public static class BasicRetryExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 1: Basic Retry (no delay) ===");

        var service = new UnreliableService("BasicService", failuresBeforeSuccess: 2);

        // Build a resilience pipeline with a simple retry strategy.
        // Polly v8 uses ResiliencePipelineBuilder — the modern, pipeline-based API.
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                // How many additional attempts after the first failure
                MaxRetryAttempts = 3,

                // Which exceptions should trigger a retry
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),

                // Called before each retry attempt
                OnRetry = args =>
                {
                    Console.WriteLine($"  Retry #{args.AttemptNumber} after: {args.Outcome.Exception?.Message}");
                    return default;
                }
            })
            .Build();

        try
        {
            var result = await pipeline.ExecuteAsync(async ct => await service.GetDataAsync(ct));
            Console.WriteLine($"  Result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  All retries exhausted: {ex.Message}");
        }

        Console.WriteLine();
    }
}
