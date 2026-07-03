using System.Diagnostics;
using Polly.CircuitBreaker;
using Polly.Timeout;

using AspNetPollyLab.Sgr;
using AspNetPollyLab.Rslc;

namespace AspNetPollyLab.FakeDep;

public record FakeDepCallResult(bool Ok, long LatencyMs, string Message); // Product of calling FakeDep

public class FakeDepCaller(FakeDep fakeDep, SgrService sgrService, RslcFakeDepPipeline rslc) // "Ambassador", "Facade", "Decorator"
{
    public async Task<FakeDepCallResult> CallOnceAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // This async lambda inside is a "single call recipe" for Polly, to repeat as many times as needed
            // Each time it has its own cancellation token, which is separate from a "ct" which goes next
            var result = await rslc.Pipeline.ExecuteAsync(async token => await fakeDep.CallFakeDepAsync(token), ct);

            return await Succeed(stopwatch, result);
        }
        catch (BrokenCircuitException)
        {
            return await Fail(stopwatch, "Fail fast: circuit is Open, dependency not touched.");
        }
        catch (TimeoutRejectedException)
        {
            return await Fail(stopwatch, "Timeout: waiting limit exceeded.");
        }
        catch (FakeDepException ex)
        {
            return await Fail(stopwatch, ex.Message);
        }
    }

    private async Task<FakeDepCallResult> Succeed(Stopwatch stopwatch, string message)
    {
        stopwatch.Stop();
        await BroadcastSgr(true, stopwatch.ElapsedMilliseconds, message);
        return new FakeDepCallResult(true, stopwatch.ElapsedMilliseconds, message);
    }

    private async Task<FakeDepCallResult> Fail(Stopwatch stopwatch, string message)
    {
        stopwatch.Stop();
        await BroadcastSgr(false, stopwatch.ElapsedMilliseconds, message);
        return new FakeDepCallResult(false, stopwatch.ElapsedMilliseconds, message);
    }

    private ValueTask BroadcastSgr(bool ok, long latencyMs, string message) =>
        sgrService.Broadcast(new SgrEvt_FakeDep_Called(ok, latencyMs, message, DateTime.UtcNow));
}
