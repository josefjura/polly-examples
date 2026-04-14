using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace PollyRetryExamples.Resilience;

/// <summary>
/// Centralised resilience policy definitions for outbound HttpClient calls.
///
/// Keeping policies in a dedicated file means:
///   - A single place to tune retry counts, delays, and timeouts.
///   - Easy to unit-test in isolation (just call Configure on a builder stub).
///   - Multiple named clients can share the same policy without duplication.
///
/// Attach to any IHttpClientBuilder via:
///   services.AddHttpClient("name")
///           .AddResilienceHandler("my-pipeline", HttpClientResiliencePolicy.Configure);
/// </summary>
public static class HttpClientResiliencePolicy
{
    /// <summary>
    /// Composes a timeout-per-attempt → retry pipeline.
    ///
    /// Pipeline execution order (inner → outer):
    ///   Request → [Retry] → [Timeout per attempt] → network
    ///
    /// <list type="bullet">
    ///   <item>Timeout: 10 s per individual attempt (throws <see cref="TimeoutRejectedException"/>).</item>
    ///   <item>Retry: up to 3 retries with exponential back-off + jitter.</item>
    ///   <item>Handles: <see cref="HttpRequestException"/>, 5xx, 408, 429 (pre-configured by <see cref="HttpRetryStrategyOptions"/>).</item>
    /// </list>
    /// </summary>
    public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        // Inner strategy: cap each individual attempt at 10 seconds.
        // A TimeoutRejectedException is treated as a transient failure by the retry layer above.
        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(10),
        });

        // Outer strategy: retry up to 3 times with exponential back-off + jitter.
        // HttpRetryStrategyOptions pre-configures ShouldHandle to cover:
        //   • HttpRequestException  (network errors, DNS failures, …)
        //   • HTTP 5xx             (server errors)
        //   • HTTP 408             (Request Timeout)
        //   • HTTP 429             (Too Many Requests)
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,

            OnRetry = args =>
            {
                var reason = args.Outcome.Exception?.Message
                             ?? $"HTTP {(int)args.Outcome.Result!.StatusCode}";
                Console.WriteLine($"    [Policy] Retry #{args.AttemptNumber} " +
                                  $"(wait {args.RetryDelay.TotalMilliseconds:F0} ms) — {reason}");
                return default;
            }
        });
    }
}
