using Microsoft.Data.Sqlite;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddSingleton<StateService>();
builder.Services.AddSingleton<ScanService>();

var app = builder.Build();

// Serve Web UI
app.UseDefaultFiles();
app.UseStaticFiles();

// ---------- ENDPOINTS ----------
app.MapGet("/", () => "Media Integrity Checker Online");

// Start scan
app.MapPost("/scan/start", async (StateService state, ScanService scan) =>
{
    if (state.Running) return Results.BadRequest("Scan already running.");
    state.Running = true;
    _ = Task.Run(() => scan.RunScan(state)); // fire and forget
    return Results.Ok("Started");
});

// Stop scan
app.MapPost("/scan/stop", (StateService state) =>
{
    state.Running = false;
    return Results.Ok("Stopping...");
});

// Get current state
app.MapGet("/state", (StateService state) =>
{
    return Results.Json(new { running = state.Running });
});

// Return log for active run
app.MapGet("/log", (StateService state) =>
{
    if (state.ActiveLogFile is null) return Results.Ok("");
    return Results.Text(File.ReadAllText(state.ActiveLogFile));
});

app.Run();