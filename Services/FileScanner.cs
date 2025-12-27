public class FileScanner
{
    private readonly DatabaseService _db;
    private readonly HashService _hash;
    private readonly IgnoreRules _ignore;
    private volatile bool _stop;

    private readonly string scanPath = Environment.GetEnvironmentVariable("SCAN_PATH") ?? "/scan";
    private readonly string logFile = "/logs/app.log";

    public FileScanner(DatabaseService db, HashService hash, IgnoreRules ignore)
    {
        _db = db;
        _hash = hash;
        _ignore = ignore;
    }

    public async Task StartScan()
    {
        _stop = false;
        Log($"=== RUN START {DateTime.Now} ===");
        Log($"Scan path: {scanPath}");

        var known = _db.LoadAll();
        var files = Directory.GetFiles(scanPath, "*.*", SearchOption.AllDirectories);
        Log($"Total files to check: {files.Length}");

        foreach (var file in files)
        {
            if (_stop) break;
            if (_ignore.ShouldIgnore(file)) continue;

            try
            {
                using var _ = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch
            {
                Log($"LOCKED: {file}", "YELLOW");
                continue;
            }

            var newHash = _hash.SHA256File(file);

            if (!known.TryGetValue(file, out var oldHash))
            {
                _db.UpsertFile(file, newHash);
                Log($"NEW: {file}", "GREEN");
                continue;
            }

            if (oldHash != newHash)
            {
                _db.UpsertFile(file, newHash);
                Log($"BAD: {file}", "RED");
            }

            known.Remove(file);
        }

        foreach (var orphan in known.Keys)
            _db.Remove(orphan);

        Log($"=== RUN END {DateTime.Now} ===");
    }

    public void RequestStop() => _stop = true;

    private void Log(string msg, string level = "INFO") =>
        File.AppendAllText(logFile, $"{level}: {msg}\n");
}