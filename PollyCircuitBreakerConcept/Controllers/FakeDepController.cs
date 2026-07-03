using Microsoft.AspNetCore.Mvc;
using PollyCircuitBreakerConcept.FakeDep;

namespace PollyCircuitBreakerConcept.Controllers;

[ApiController]
[Route("api")]
public class FakeDepController(FakeDepCaller fakeDepCaller, FakeDepState fakeDepState) : ControllerBase // One instance per pequest
{
    [HttpGet("callFakeDep")]
    public async Task<IActionResult> CallFakeDep(CancellationToken cancellationToken)
    {
        //Here, cancellationToken may come from HttpContext.RequestAborted 
        var result = await fakeDepCaller.CallOnceAsync(cancellationToken);
        return result.Ok
            ? Ok(new { ok = true, latencyMs = result.LatencyMs, result = result.Message })
            : StatusCode(502, new { ok = false, latencyMs = result.LatencyMs, error = result.Message });
    }

    [HttpPatch("setFakeDepState")]
    public IActionResult SetFakeDepState([FromBody] FakeDepPatch patch) // Get JSON from request body, not from URL
    {
        if (patch.IsUp is { } up) fakeDepState.IsUp = up;
        if (patch.DelayMs is { } delay) fakeDepState.DelayMs = Math.Max(0, delay); // Simple validating shield
        if (patch.FailurePercentage is { } percentage) fakeDepState.FailurePercentage = Math.Clamp(percentage, 0, 100);
        return Ok(fakeDepState);
    }
}

public record FakeDepPatch(bool? IsUp, int? DelayMs, int? FailurePercentage); // No OpenAPI / Swagger here, manual JS
