using Polly;
using Polly.Retry;
using PollyRetryExamples.Services;

namespace PollyRetryExamples.Examples;

/// <summary>
/// Example 3: Exponential backoff — the delay doubles with each retry attempt.
/// Best for: avoiding the "thundering herd" problem when many clients hammer
/// a struggling service. Standard pattern for HTTP APIs and message queues.
///
/// Delay schedule (base 1s, no jitter):
///   Attempt 1 → wait 1s
///   Attempt 2 → wait 2s
///   Attempt 3 → wait 4s
///   Attempt 4 → wait 8s
/// </summary>
public static class ExponentialBackoffExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 3: Exponential Backoff ===");

        var service = new UnreliableService("ExponentialService", failuresBeforeSuccess: 3);

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 4,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),

                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,

                // UseJitter = false here so the doubling pattern is clear in the output.
                // In production you almost always want UseJitter = true (see next example).
                UseJitter = false,

                OnRetry = args =>
                {
                    Console.WriteLine($"  Retry #{args.AttemptNumber} — waiting {args.RetryDelay.TotalSeconds:F1}s");
                    return default;
                }
            })
            .Build();

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await pipeline.ExecuteAsync(async ct => await service.GetDataAsync(ct));
            stopwatch.Stop();
            Console.WriteLine($"  Result: {result} (total elapsed: {stopwatch.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  All retries exhausted: {ex.Message}");
        }

        Console.WriteLine();
    }
}
