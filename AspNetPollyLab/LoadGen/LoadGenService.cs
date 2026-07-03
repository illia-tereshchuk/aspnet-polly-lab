using AspNetPollyLab.FakeDep;

namespace AspNetPollyLab.LoadGen;

/// <summary>
/// Background loop that continuously hits the dependency at the configured rate while enabled.
/// Calls are fire-and-forget: a slow dependency does not stall the generator itself; instead
/// requests pile up in flight (exactly the highload drama we want to show).
/// </summary>
public class LoadGenService(LoadGenState state, FakeDepCaller fakeDepCaller) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (state.IsRunning && state.RequestsPerSecond > 0)
            {
                // Fire and forget, not awaiting
                // Safe, because exceptions are all caught inside
                _ = fakeDepCaller.CallOnceAsync(stoppingToken);
                
                var intervalMs = Math.Max(1, 1000 / state.RequestsPerSecond);
                await SafeDelay(intervalMs, stoppingToken);
            }
            else
            {
                await SafeDelay(100, stoppingToken); // Idle: check state 10 times/sec
            }
        }
    }

    private static async Task SafeDelay(int ms, CancellationToken ct) // Just to isolate "OperationCanceledException"
    {
        try { await Task.Delay(ms, ct); }
        catch (OperationCanceledException) { }
    }
}

public class LoadGenState
{
    public bool IsRunning { get; set; } = false;

    public int RequestsPerSecond { get; set; } = 5;
}