using Polly;
using Polly.Retry;
using PollyRetryExamples.Services;

namespace PollyRetryExamples.Examples.Retry;

/// <summary>
/// Example 5: Retry based on the RESULT of the operation, not just exceptions.
/// Best for: APIs that signal transient failure via return values rather than
/// exceptions — e.g. returning -1 / null / an "unavailable" status code.
///
/// Also demonstrates retrying on specific exception types only (not all exceptions),
/// and a maximum total duration cap instead of a fixed retry count.
/// </summary>
public static class RetryOnResultExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Example 5: Retry on Exception Type + Bad Result ===");

        var service = new UnreliableService("ResultService", failuresBeforeSuccess: 3);

        // ResiliencePipelineBuilder<T> is used when you need to inspect the return value.
        var pipeline = new ResiliencePipelineBuilder<int>()
            .AddRetry(new RetryStrategyOptions<int>
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromMilliseconds(300),
                BackoffType = DelayBackoffType.Constant,

                // Trigger retry on:
                //   (a) specific exception types
                //   (b) a result that indicates failure (-1 in this demo)
                ShouldHandle = new PredicateBuilder<int>()
                    .Handle<InvalidOperationException>()   // transient exception
                    .HandleResult(value => value < 0),     // bad result value

                OnRetry = args =>
                {
                    if (args.Outcome.Exception is not null)
                        Console.WriteLine($"  Retry #{args.AttemptNumber} — exception: {args.Outcome.Exception.Message}");
                    else
                        Console.WriteLine($"  Retry #{args.AttemptNumber} — bad result: {args.Outcome.Result}");
                    return default;
                }
            })
            .Build();

        try
        {
            var value = await pipeline.ExecuteAsync(async ct => await service.GetValueAsync(ct));
            Console.WriteLine($"  Final value: {value}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  All retries exhausted: {ex.Message}");
        }

        Console.WriteLine();
    }
}
