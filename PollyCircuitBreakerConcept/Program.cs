using PollyCircuitBreakerConcept.FakeDep;
using PollyCircuitBreakerConcept.LoadGen;
using PollyCircuitBreakerConcept.Sgr;
using PollyCircuitBreakerConcept.Rslc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSignalR();

builder.Services.AddSingleton<FakeDepState>(); // One instance per app
builder.Services.AddSingleton<FakeDep>();
builder.Services.AddSingleton<FakeDepCaller>();

builder.Services.AddSingleton<RslcFakeDepPipeline>();

builder.Services.AddSingleton<SgrService>();

builder.Services.AddSingleton<LoadGenState>();
builder.Services.AddHostedService<LoadGenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();   // give index.html to "/"
app.UseStaticFiles();    // populate wwwroot

app.MapControllers();
app.MapHub<SgrHub>("/sgrHub");

app.Run();
