using Polly;
using Polly.Retry;
using PollyRetryExamples.Services;

namespace PollyRetryExamples.Examples.Retry;

/// <summary>
/// Example 4: Exponential backoff WITH jitter.
/// Best for: production systems where many concurrent clients retry simultaneously.
///
/// Without jitter, all clients that hit an outage at the same time will retry
/// in perfect synchrony — creating a "retry storm" that overwhelms the recovering
/// service just as it comes back up.
///
/// With UseJitter = true, Polly applies the "decorrelated jitter" formula
/// (based on the AWS Architecture Blog recommendation) which spreads retries
/// across a random window around the base delay.
/// </summary>
public static class JitterRetryExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 4: Exponential Backoff with Jitter ===");
        Console.WriteLine("  (simulating 3 concurrent clients to show spread)");
        Console.WriteLine();

        // Simulate three clients hitting the same broken service simultaneously
        var tasks = Enumerable.Range(1, 3).Select(clientId => SimulateClientAsync(clientId));
        await Task.WhenAll(tasks);

        Console.WriteLine();
    }

    private static async Task SimulateClientAsync(int clientId)
    {
        // Each client gets its own independent service + pipeline
        var service = new UnreliableService($"Client-{clientId}", failuresBeforeSuccess: 2);

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),

                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true, // <-- spreads retries across a random window

                OnRetry = args =>
                {
                    Console.WriteLine($"  [Client-{clientId}] Retry #{args.AttemptNumber}, " +
                                      $"actual wait: {args.RetryDelay.TotalMilliseconds:F0}ms");
                    return default;
                }
            })
            .Build();

        try
        {
            var result = await pipeline.ExecuteAsync(async ct => await service.GetDataAsync(ct));
            Console.WriteLine($"  [Client-{clientId}] {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [Client-{clientId}] Failed: {ex.Message}");
        }
    }
}
