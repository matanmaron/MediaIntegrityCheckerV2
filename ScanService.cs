using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using Dapper;

public class ScanService
{
    private readonly string dbPath = "Data/checksums.db";
    private readonly string logDir = "Logs";

    public ScanService()
    {
        Directory.CreateDirectory("Data");
        Directory.CreateDirectory("Logs");
        InitDb();
        CleanupOldLogs();
    }

    public void RunScan(StateService state)
    {
        // create new log for this run
        var logFile = $"{logDir}/scan_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
        state.ActiveLogFile = logFile;

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        var files = Directory.GetFiles(state.TargetPath, "*.*", SearchOption.AllDirectories);

        Log(logFile, $"RUN {DateTime.Now} | FILES: {files.Length}");

        foreach (var file in files)
        {
            if (!state.Running) break; // allow STOP button to interrupt

            try
            {
                var info = new FileInfo(file);
                var lastWrite = info.LastWriteTimeUtc;
                var size = info.Length;

                var existing = conn.Query<FileRecord>("SELECT * FROM Files WHERE Path = @p", new { p = file }).FirstOrDefault();

                if (existing != null && existing.LastModified == lastWrite && existing.Size == size)
                    continue;

                // file changed or new -> hash now
                var hash = ComputeSHA256(file);

                if (existing == null)
                {
                    conn.Execute("INSERT INTO Files(Path,Hash,Size,LastModified) VALUES(@Path,@Hash,@Size,@LastModified)",
                        new { Path = file, Hash = hash, Size = size, LastModified = lastWrite });
                    Log(logFile, $"[NEW] {file}");
                }
                else
                {
                    if (existing.Hash != hash)
                        Log(logFile, $"[BAD] {file}");
                    else
                        Log(logFile, $"[OK ] {file}");

                    conn.Execute("UPDATE Files SET Hash=@Hash, Size=@Size, LastModified=@LastModified WHERE Path=@Path",
                        new { Path = file, Hash = hash, Size = size, LastModified = lastWrite });
                }
            }
            catch
            {
                Log(logFile, $"[LOCK] {file}");
            }
        }

        Log(logFile, $"FINISHED {DateTime.Now}");
        state.Running = false;
    }

    private void InitDb()
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Files(
                Path TEXT PRIMARY KEY,
                Hash TEXT,
                Size INTEGER,
                LastModified TEXT
            );
        ");
    }

    private string ComputeSHA256(string file)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(file);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }

    private void CleanupOldLogs()
    {
        var files = Directory.GetFiles(logDir, "*.log");
        foreach (var f in files)
            if (File.GetCreationTime(f) < DateTime.Now.AddDays(-30))
                File.Delete(f);
    }

    private void Log(string file, string text)
    {
        File.AppendAllText(file, text + "\n");
    }
}
