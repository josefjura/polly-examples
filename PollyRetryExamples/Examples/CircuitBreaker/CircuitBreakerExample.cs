using Polly;
using Polly.CircuitBreaker;

namespace PollyRetryExamples.Examples.CircuitBreaker;

/// <summary>
/// Example 8: Circuit Breaker — stops forwarding calls to a failing service so it
/// can recover instead of being overwhelmed by a flood of retries.
///
/// States:
///   Closed    → normal operation; failures are counted inside the sampling window
///   Open      → fast-fails immediately (BrokenCircuitException); no calls reach the service
///   Half-Open → one trial call is allowed; success closes the circuit, failure reopens it
///
/// Best for: protecting downstream services from cascading failure and giving them
/// breathing room to recover during a sustained outage.
/// </summary>
public static class CircuitBreakerExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 8: Circuit Breaker ===");

        var shouldFail = true;
        var callCount = 0;

        async Task<string> CallServiceAsync(CancellationToken ct)
        {
            callCount++;
            await Task.Delay(30, ct); // simulate network latency
            if (shouldFail)
                throw new HttpRequestException($"Service unavailable (call #{callCount})");
            return $"Success (call #{callCount})";
        }

        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                // Open once 60 % of calls within the sampling window have failed
                FailureRatio = 0.6,

                // Sliding window used to measure the failure rate
                SamplingDuration = TimeSpan.FromSeconds(10),

                // Don't trip the breaker until at least this many calls have been made
                MinimumThroughput = 3,

                // How long the circuit stays open before moving to Half-Open
                BreakDuration = TimeSpan.FromSeconds(2),

                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),

                OnOpened = args =>
                {
                    Console.WriteLine($"  *** Circuit OPENED — breaking for {args.BreakDuration.TotalSeconds}s " +
                                      $"(last error: {args.Outcome.Exception?.Message}) ***");
                    return default;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("  *** Circuit HALF-OPEN — sending one trial request ***");
                    return default;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("  *** Circuit CLOSED — service is healthy again ***");
                    return default;
                }
            })
            .Build();

        // Phase 1: calls reach the service and fail → enough failures to trip the breaker
        Console.WriteLine("\n  [Phase 1] Calling a failing service — circuit will open after threshold...");
        for (int i = 1; i <= 5; i++)
        {
            try
            {
                var result = await pipeline.ExecuteAsync(async ct => await CallServiceAsync(ct));
                Console.WriteLine($"    Call {i}: {result}");
            }
            catch (BrokenCircuitException)
            {
                Console.WriteLine($"    Call {i}: Circuit is OPEN — fast-failed (service not contacted)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Call {i}: {ex.Message}");
            }
        }

        // Phase 2: let the service "recover" and wait for the break duration to elapse
        Console.WriteLine($"\n  [Phase 2] Service recovered. Waiting for break duration to elapse...");
        shouldFail = false;
        await Task.Delay(TimeSpan.FromSeconds(2.5));

        // Phase 3: circuit moves to Half-Open, trial call succeeds, circuit closes
        Console.WriteLine("  [Phase 3] Retrying after break — circuit should recover...");
        for (int i = 1; i <= 3; i++)
        {
            try
            {
                var result = await pipeline.ExecuteAsync(async ct => await CallServiceAsync(ct));
                Console.WriteLine($"    Call {i}: {result}");
            }
            catch (BrokenCircuitException)
            {
                Console.WriteLine($"    Call {i}: Circuit still OPEN — fast-failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Call {i}: {ex.Message}");
            }
        }

        Console.WriteLine();
    }
}
