namespace AspNetPollyLab.FakeDep;

public class FakeDep(FakeDepState fakeDepState) // Primary constructor with "backing field", C# 12
{
    public async Task<string> CallFakeDepAsync(CancellationToken ct = default)
    {
        // Polly resilience pipeline from "FakeDepCaller" passes "ct" here with "async token =>" 

        if (!fakeDepState.IsUp)
            throw new FakeDepException("Fake dependency is down (turned off).");

        await Task.Delay(fakeDepState.DelayMs, ct); // Slow response, pretend to consume resources

        // Next(100) yields 0..99, so "< N" fires with exactly an N% chance.
        if (Random.Shared.Next(100) < fakeDepState.FailurePercentage)
            throw new FakeDepException("Fake dependency error.");

        // NOTE: If cancelled with "ct", then "OperationCanceledException" is thrown

        return $"OK from fake dependency at {DateTime.UtcNow:HH:mm:ss.fff}";
    }
}

public class FakeDepState // Lives in single instance while controllers recreate
{
    public bool IsUp { get; set; } = true;
    public int DelayMs { get; set; } = 50;
    public int FailurePercentage { get; set; }
}

public class FakeDepException(string message) : Exception(message);