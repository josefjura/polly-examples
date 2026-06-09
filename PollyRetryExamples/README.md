# Polly Resilience Examples

Runnable examples of every major [Polly v8](https://www.pollydocs.org/) resilience strategy, written with the modern `ResiliencePipelineBuilder` API.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
dotnet run
```

---

## Examples

### Retry (`Examples/Retry/`)

All retry examples use [`ResiliencePipelineBuilder`](https://www.pollydocs.org/pipelines/index.html) with [`RetryStrategyOptions`](https://www.pollydocs.org/strategies/retry.html).

| # | File | What it shows |
|---|------|---------------|
| 1 | `BasicRetryExample.cs` | Immediate retry with no delay — simplest possible policy |
| 2 | `WaitAndRetryExample.cs` | Fixed delay between attempts |
| 3 | `ExponentialBackoffExample.cs` | Delay doubles each attempt (`BackoffType.Exponential`, no jitter) |
| 4 | `JitterRetryExample.cs` | Exponential backoff with decorrelated jitter (`UseJitter = true`) across 3 concurrent clients |
| 5 | `RetryOnResultExample.cs` | Retry triggered by a bad *return value* (not just exceptions) using `HandleResult` |
| 6 | `HttpClientRetryExample.cs` | `ResiliencePipeline<HttpResponseMessage>` wrapping `HttpClient`; retries on network errors and non-2xx status codes |
| 7 | `AddResilienceHandlerExample.cs` | DI-native `AddResilienceHandler` with the policy split into its own file (`Resilience/HttpClientResiliencePolicy.cs`) |

**Docs:** [strategies/retry](https://www.pollydocs.org/strategies/retry.html) · [pipelines](https://www.pollydocs.org/pipelines/index.html) · [dependency injection](https://www.pollydocs.org/advanced/dependency-injection.html)

---

### Circuit Breaker (`Examples/CircuitBreaker/`)

| # | File | What it shows |
|---|------|---------------|
| 8 | `CircuitBreakerExample.cs` | Full state-machine walkthrough: Closed → Open (fast-fail) → Half-Open (trial) → Closed |

Configured with `FailureRatio`, `MinimumThroughput`, `SamplingDuration`, and `BreakDuration`. State transitions are logged via `OnOpened`, `OnHalfOpened`, and `OnClosed` callbacks.

**Docs:** [strategies/circuit-breaker](https://www.pollydocs.org/strategies/circuit-breaker.html)

---

### Rate Limiter (`Examples/RateLimiter/`)

| # | File | What it shows |
|---|------|---------------|
| 9 | `RateLimiterExample.cs` | Token bucket (`TokenBucketRateLimiter`) — 3 tokens consumed, 3 calls rejected with `RateLimiterRejectedException`, then replenishment and recovery |

Polly v8 wraps `System.Threading.RateLimiting` directly, so any built-in limiter works: `FixedWindowRateLimiter`, `SlidingWindowRateLimiter`, `TokenBucketRateLimiter`, or `ConcurrencyLimiter`.

**Docs:** [strategies/rate-limiter](https://www.pollydocs.org/strategies/rate-limiter.html)

---

### Fallback (`Examples/Fallback/`)

| # | File | What it shows |
|---|------|---------------|
| 10 | `FallbackExample.cs` | (A) Bare fallback returning a cached value when the service is down; (B) Retry × 2 then fallback — demonstrates inner-to-outer pipeline execution order |

**Docs:** [strategies/fallback](https://www.pollydocs.org/strategies/fallback.html) · [resilience strategies overview](https://www.pollydocs.org/strategies/index.html)

---

## Project structure

```
PollyRetryExamples/
├── Examples/
│   ├── Retry/
│   ├── CircuitBreaker/
│   ├── RateLimiter/
│   └── Fallback/
├── Resilience/
│   └── HttpClientResiliencePolicy.cs   # shared HttpClient policy used by example 7
├── Services/
│   └── UnreliableService.cs            # simulates a flaky dependency
└── Program.cs
```

## Further reading

- [Getting started](https://www.pollydocs.org/getting-started.html)
- [Resilience strategies](https://www.pollydocs.org/strategies/index.html)
- [Resilience pipelines](https://www.pollydocs.org/pipelines/index.html)
- [Telemetry and monitoring](https://www.pollydocs.org/advanced/telemetry.html)
- [Testing](https://www.pollydocs.org/advanced/testing.html)
- [Performance](https://www.pollydocs.org/advanced/performance.html)
