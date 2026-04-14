using Polly;
using Polly.Retry;
using PollyRetryExamples.Services;

namespace PollyRetryExamples.Examples;

/// <summary>
/// Example 2: Wait-and-retry with a fixed delay between each attempt.
/// Best for: remote calls where the downstream service needs a moment to recover
/// (e.g. a restarting microservice, a briefly overloaded DB).
/// </summary>
public static class WaitAndRetryExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 2: Wait-and-Retry (fixed delay) ===");

        var service = new UnreliableService("FixedDelayService", failuresBeforeSuccess: 2);

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),

                // Fixed pause between retries
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Constant,

                OnRetry = args =>
                {
                    Console.WriteLine($"  Retry #{args.AttemptNumber} in {args.RetryDelay.TotalMilliseconds}ms " +
                                      $"— reason: {args.Outcome.Exception?.Message}");
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
