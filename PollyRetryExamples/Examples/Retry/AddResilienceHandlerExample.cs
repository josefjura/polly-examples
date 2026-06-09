using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using PollyRetryExamples.Resilience;

namespace PollyRetryExamples.Examples.Retry;

/// <summary>
/// Example 7: AddResilienceHandler — the modern, DI-native way to attach a Polly
/// resilience pipeline to an IHttpClientFactory-managed HttpClient.
///
/// Key differences from the older AddPolicyHandler (Microsoft.Extensions.Http.Polly):
///   • Uses the Polly v8 ResiliencePipeline&lt;HttpResponseMessage&gt; API.
///   • Pipelines are composable (retry, circuit breaker, timeout in one builder).
///   • HttpRetryStrategyOptions / HttpTimeoutStrategyOptions pre-wire HTTP-aware defaults.
///   • The policy lives in a separate file (HttpClientResiliencePolicy) so it can be
///     shared, tested, and configured independently of the registration code.
///
/// Best for: ASP.NET Core / Generic Host apps where HttpClient is already managed by DI.
/// </summary>
public static class AddResilienceHandlerExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 7: AddResilienceHandler with Split Policy File ===");
        Console.WriteLine();

        // ── Service registration ───────────────────────────────────────────────
        // This block mirrors what you would write in Program.cs / Startup.cs.
        // The policy itself is defined in Resilience/HttpClientResiliencePolicy.cs.
        var services = new ServiceCollection();

        services
            .AddHttpClient("WeatherApi", client =>
            {
                client.BaseAddress = new Uri("https://httpbin.org/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            // "http-resilience-pipeline" is the name used to identify this handler
            // in logs and metrics. The second argument delegates to the shared policy.
            .AddResilienceHandler("http-resilience-pipeline", HttpClientResiliencePolicy.Configure);

        await using var provider = services.BuildServiceProvider();

        // ── Happy path: 200 OK ─────────────────────────────────────────────────
        Console.WriteLine("  [A] Happy path — request that succeeds immediately:");
        await ExecuteRequestAsync(provider, "status/200");

        Console.WriteLine();

        // ── Transient failure: 503 → retries → eventual success ────────────────
        // httpbin.org/status/503 always returns 503, so all retries will be
        // exhausted. In a real app the service would recover on a later attempt.
        Console.WriteLine("  [B] Transient failure — 503 triggers retries (all will exhaust here):");
        await ExecuteRequestAsync(provider, "status/503");

        Console.WriteLine();
    }

    private static async Task ExecuteRequestAsync(IServiceProvider provider, string path)
    {
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        // The named client already has the resilience pipeline attached —
        // no manual pipeline.ExecuteAsync() call needed.
        var client = factory.CreateClient("WeatherApi");

        try
        {
            var response = await client.GetAsync(path);
            Console.WriteLine($"    Final status: {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    All retries exhausted: {ex.GetType().Name} — {ex.Message}");
        }
    }
}
