using System.Data;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using AspNetPollyLab.FakeDep;
using AspNetPollyLab.Sgr;

namespace AspNetPollyLab.Rslc;

/// <summary>
/// Resilience layer around the dependency call.
/// Order outer-to-inner:
/// CircuitBreaker → Retry → Timeout.
/// All of Polly lives here.
/// </summary>
public class RslcFakeDepPipeline
{
    public ResiliencePipeline Pipeline { get; }

    public RslcFakeDepPipeline(SgrService sgrService)
    {
        Pipeline = new ResiliencePipelineBuilder()
            // This is the first "gate keeper". Circuit breaker decides, if the call does next at all. It's "fail-fast"
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()       // What should be considered as a failure
                    .Handle<FakeDepException>()             // No "OperationCanceledException": if client cancels, no problem
                    .Handle<TimeoutRejectedException>(),    // No "BrokenCircuitException" to avoid infinite self-loop
                FailureRatio = 0.5,                         // When those from "ShouldHandle" are triggered more that >= 50% of attempts
                MinimumThroughput = 5,                      // At least 5 calls to react and start counting failure statistics
                SamplingDuration = TimeSpan.FromSeconds(10),// NOTE: A piece of time, where we had at least 5 attempts with 50% failures
                BreakDuration = TimeSpan.FromSeconds(5),    // Cooldown after triggering before becoming "HalfOpen"
                // When "HalfOpen", the very next call, depending on success or failure, judges to unlock on keep locking for 5s again 
                OnOpened = args => Sgr_Circuit("Open", $"opened for {args.BreakDuration.TotalSeconds:0}s (too many failures)"),
                OnClosed = _ => Sgr_Circuit("Closed", "closed — traffic restored"),
                OnHalfOpened = _ => Sgr_Circuit("HalfOpen", "trial request")
            })
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<FakeDepException>()
                    .Handle<TimeoutRejectedException>(),    // Also no "BrokenCircuitException": no need to retry when circuit is broken
                MaxRetryAttempts = 3,                       // 1 call + 3 retries = 4 calls total, 3 retries are not 3 calls
                BackoffType = DelayBackoffType.Exponential, // 200 -> 400 -> 800 (200x1, 200x2, 200x4, 200x16[no], 200x256[no])
                // Also consider "UseJitter" option: it randomizes pauses to avoid mass retry storm in the same time from many users
                Delay = TimeSpan.FromMilliseconds(200),
                OnRetry = args =>
                {
                    var attemptNumber = args.AttemptNumber + 1; // Because it's zero-based
                    var reason = args.Outcome.Exception?.Message ?? "unknown"; // Exception is 100% here, but message - questionable
                    var delay = args.RetryDelay; // Delay before next retry (not using here, but can broadcast also)

                    return Sgr_Retry(attemptNumber, reason);
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(1))            // Each attempt has 1s to execute at all
            // If dependency is slow (1500ms), "TimeoutRejectedException" is thrown, and it propagates to "Retry"
            // "Retry" block with its logic propagates upper to "CitcuitBreaker"
            .Build();

        // Shortcut delegates to broadcast events to SignalR
        // Can be improved with adapter like "IResilienceObserver" and so on 
        // Also "builder.ConfigureTelemetry(...)" from "Polly.Extensions", but it's too much for demo
        ValueTask Sgr_Circuit(string state, string detail) =>
            sgrService.Broadcast(new SgrEvt_CircuitState_Changed(state, detail, DateTime.UtcNow));

        ValueTask Sgr_Retry(int attemptNumber, string reason) =>
            sgrService.Broadcast(new SgrEvt_FakeDepCall_Retried(attemptNumber, reason, DateTime.UtcNow));
    }
}
