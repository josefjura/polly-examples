namespace PollyRetryExamples.Services;

/// <summary>
/// Simulates an unreliable external dependency (e.g. a flaky API or database).
/// The service fails a configurable number of times before succeeding.
/// </summary>
public class UnreliableService
{
    private int _callCount = 0;
    private readonly int _failuresBeforeSuccess;
    private readonly string _name;

    public UnreliableService(string name, int failuresBeforeSuccess = 2)
    {
        _name = name;
        _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public void Reset()
    {
        _callCount = 0;
    }

    public async Task<string> GetDataAsync(CancellationToken ct = default)
    {
        _callCount++;
        Console.WriteLine($"  [{_name}] Attempt #{_callCount}...");
        await Task.Delay(50, ct); // simulate network latency

        if (_callCount <= _failuresBeforeSuccess)
        {
            throw new HttpRequestException($"Service unavailable (attempt {_callCount})");
        }

        return $"Success on attempt #{_callCount}";
    }

    public async Task<int> GetValueAsync(CancellationToken ct = default)
    {
        _callCount++;
        Console.WriteLine($"  [{_name}] Attempt #{_callCount}...");
        await Task.Delay(50, ct);

        // Fail first few calls, then return a bad value, then eventually succeed
        if (_callCount <= _failuresBeforeSuccess - 1)
            throw new InvalidOperationException($"Transient error on attempt {_callCount}");

        if (_callCount == _failuresBeforeSuccess)
            return -1; // bad result — triggers result-based retry

        return 42;
    }
}
