namespace PollyCircuitBreakerConcept.Sgr;

public record SgrEvt_FakeDep_Called(bool Ok, long LatencyMs, string Message, DateTime At);

public record SgrEvt_FakeDepCall_Retried(int AttemptNumber, string Reason, DateTime At);

public record SgrEvt_CircuitState_Changed(string State, string Detail, DateTime At); // State: "Closed" | "Open" | "HalfOpen"
