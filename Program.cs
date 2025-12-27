using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<HashService>();
builder.Services.AddSingleton<IgnoreRules>();
builder.Services.AddSingleton<FileScanner>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// API: Start scan
app.MapPost("/scan/start", async (FileScanner scanner) =>
{
    await scanner.StartScan();
    return Results.Ok("Scan started");
});

// API: Stop scan (not instant, cooperative stop)
app.MapPost("/scan/stop", (FileScanner scanner) =>
{
    scanner.RequestStop();
    return Results.Ok("Stop requested");
});

// API: Get log
app.MapGet("/log", () =>
{
    return Results.Text(System.IO.File.ReadAllText("/logs/app.log"));
});

// Static Web UI
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();