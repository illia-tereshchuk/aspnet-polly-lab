using Microsoft.AspNetCore.Mvc;
using PollyCircuitBreakerConcept.LoadGen;

namespace PollyCircuitBreakerConcept.Controllers;

[ApiController]
[Route("api")]
public class LoadGenController(LoadGenState state) : ControllerBase
{
    [HttpPatch("setLoadGenState")]
    public IActionResult SetLoadGenState([FromBody] LoadGenPatch patch)
    {
        if (patch.IsRunning is { } running) state.IsRunning = running;
        if (patch.RequestsPerSecond is { } reqPerSec) state.RequestsPerSecond = Math.Clamp(reqPerSec, 1, 100);
        return Ok(state);
    }
}

public record LoadGenPatch(bool? IsRunning, int? RequestsPerSecond);
