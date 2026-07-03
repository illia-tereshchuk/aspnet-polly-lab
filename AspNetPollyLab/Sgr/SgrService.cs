using Microsoft.AspNetCore.SignalR;

namespace AspNetPollyLab.Sgr;

public class SgrService(IHubContext<SgrHub> hubCtx)
{
    public ValueTask Broadcast<TEvent>(TEvent payload) =>
        new(hubCtx.Clients.All.SendAsync(EventName(typeof(TEvent)), payload));

    private static string EventName(Type type) // SgrEvt_FakeDep_Called -> "fakeDep_Called"
    {
        var name = type.Name;
        if (name.StartsWith("SgrEvt_"))
            name = name["SgrEvt_".Length..];
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
