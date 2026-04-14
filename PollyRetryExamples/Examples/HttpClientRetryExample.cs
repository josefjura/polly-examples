using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace PollyRetryExamples.Examples;

/// <summary>
/// Example 6: Retry policy applied to HttpClient via IHttpClientFactory.
/// Best for: any production app calling HTTP APIs — integrates cleanly with
/// ASP.NET Core / Generic Host dependency injection.
///
/// Uses a typed HttpResponseMessage pipeline so both network exceptions AND
/// non-success HTTP status codes (429, 503, etc.) trigger retries.
/// </summary>
public static class HttpClientRetryExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 6: HttpClient with Retry Pipeline ===");

        // --- Approach A: Manual pipeline wrapping a plain HttpClient ---
        await DemonstrateManualPipelineAsync();

        // --- Approach B: IHttpClientFactory registered in DI ---
        await DemonstrateDiRegistrationAsync();

        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // Approach A: wrap HttpClient calls manually in a ResiliencePipeline<HttpResponseMessage>
    // -------------------------------------------------------------------------
    private static async Task DemonstrateManualPipelineAsync()
    {
        Console.WriteLine("  [A] Manual pipeline wrapping HttpClient");

        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,

                // Retry on network errors OR non-2xx responses
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests),

                OnRetry = args =>
                {
                    var reason = args.Outcome.Exception?.Message
                                 ?? $"HTTP {(int)args.Outcome.Result!.StatusCode}";
                    Console.WriteLine($"    Retry #{args.AttemptNumber} ({args.RetryDelay.TotalMilliseconds:F0}ms) — {reason}");
                    return default;
                }
            })
            .Build();

        using var httpClient = new HttpClient();

        try
        {
            // Using a URL that reliably returns 200 to show a success path.
            // In a real app the service might return 503 occasionally.
            var response = await pipeline.ExecuteAsync(async ct =>
                await httpClient.GetAsync("https://httpbin.org/status/200", ct));

            Console.WriteLine($"    Final status: {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    All retries exhausted: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Approach B: register the policy in DI and inject a named HttpClient
    // -------------------------------------------------------------------------
    private static async Task DemonstrateDiRegistrationAsync()
    {
        Console.WriteLine("  [B] IHttpClientFactory registered in DI");

        // Build the shared retry pipeline once and reuse it.
        var retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnRetry = args =>
                {
                    Console.WriteLine($"    [DI Client] Retry #{args.AttemptNumber}");
                    return default;
                }
            })
            .Build();

        // Register services
        var services = new ServiceCollection();
        services.AddHttpClient("MyApiClient", client =>
        {
            client.BaseAddress = new Uri("https://httpbin.org/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient("MyApiClient");

        try
        {
            // Manually execute the pipeline with the DI-provided client.
            // (With Microsoft.Extensions.Http.Resilience you can attach the pipeline
            //  directly during AddHttpClient — see the project README for details.)
            var response = await retryPipeline.ExecuteAsync(async ct =>
                await httpClient.GetAsync("get", ct));

            Console.WriteLine($"    Final status: {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    All retries exhausted: {ex.Message}");
        }
    }
}
