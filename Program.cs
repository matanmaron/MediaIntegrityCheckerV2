using Microsoft.Data.Sqlite;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// Read scan path from config / Docker env
var scanPath = builder.Configuration.GetValue<string>("ScanDirectory");
if (string.IsNullOrWhiteSpace(scanPath) || !Directory.Exists(scanPath))
{
    throw new Exception($"Configured scan path does not exist: {scanPath}. Fix the Docker volume mapping.");
}

// Create services
builder.Services.AddSingleton<ScanState>();
builder.Services.AddSingleton(new ScanService(scanPath));

var app = builder.Build();

// Serve static UI files
app.UseDefaultFiles();
app.UseStaticFiles();

// ---------- ENDPOINTS ----------

// START SCAN
app.MapPost("/scan/start", (ScanService scanner, ScanState state) =>
{
    if (state.IsRunning) return Results.BadRequest("Scan already running.");

    // Create new log for this run
    state.LogFile = $"logs/Run_{DateTime.Now:yyyyMMdd_HHmmss}.log";
    Directory.CreateDirectory("logs");
    File.WriteAllText(state.LogFile, $"=== SCAN STARTED {DateTime.Now} ===\n");

    state.IsRunning = true;
    state.TokenSource = new CancellationTokenSource();

    _ = Task.Run(() => scanner.RunChecksumScan(state.TokenSource.Token, state.LogFile));

    return Results.Ok("Scan started");
});

// STOP SCAN
app.MapPost("/scan/stop", (ScanState state) =>
{
    if (!state.IsRunning) return Results.BadRequest("No scan running.");
    state.TokenSource.Cancel();
    state.IsRunning = false;
    File.AppendAllText(state.LogFile, "\n=== SCAN STOPPED ===\n");
    return Results.Ok("Scan stopping...");
});

// STATE for UI
app.MapGet("/state", (ScanState state) =>
{
    return Results.Json(new { running = state.IsRunning, logfile = state.LogFile });
});

// READ LOG
app.MapGet("/log", (ScanState state) =>
{
    if (state.LogFile == null || !File.Exists(state.LogFile)) return Results.Ok("");
    return Results.Text(File.ReadAllText(state.LogFile));
});

app.Run();