using PollyRetryExamples.Examples;

Console.WriteLine("Polly Retry Policy Examples");
Console.WriteLine("============================");
Console.WriteLine("Polly v8 uses ResiliencePipeline — a unified, composable resilience API.");
Console.WriteLine("Each example below demonstrates a different retry strategy.");
Console.WriteLine();

// 1. Immediate retry — no delay, just try again
await BasicRetryExample.RunAsync();

// 2. Fixed delay between each attempt
await WaitAndRetryExample.RunAsync();

// 3. Exponential backoff — delay doubles each time (no jitter, for clarity)
await ExponentialBackoffExample.RunAsync();

// 4. Exponential backoff WITH jitter — prevents retry storms in concurrent scenarios
await JitterRetryExample.RunAsync();

// 5. Retry triggered by bad *results* as well as exceptions
await RetryOnResultExample.RunAsync();

// 6. HttpClient integration — retry on network errors and non-2xx HTTP responses
await HttpClientRetryExample.RunAsync();

// 7. AddResilienceHandler — modern DI-native approach with policy split to its own file
await AddResilienceHandlerExample.RunAsync();

Console.WriteLine("All examples completed.");
