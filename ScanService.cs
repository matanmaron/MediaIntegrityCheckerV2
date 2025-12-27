using Microsoft.Data.Sqlite;
using Dapper;
using System.Security.Cryptography;

public class ScanService
{
    private readonly string dbPath = "data/checksums.db";
    private readonly string targetFolder;

    public ScanService(string scanPath)
    {
        targetFolder = scanPath;
        Directory.CreateDirectory("data");

        // Initialize SQLite DB if not exists
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

    public void RunChecksumScan(CancellationToken token, string logFile)
    {
        var files = Directory.GetFiles(targetFolder, "*.*", SearchOption.AllDirectories);

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        foreach (var file in files)
        {
            if (token.IsCancellationRequested) break;

            try
            {
                var info = new FileInfo(file);
                var lastWrite = info.LastWriteTimeUtc;
                var size = info.Length;

                var existing = conn.QueryFirstOrDefault<FileRecord>(
                    "SELECT * FROM Files WHERE Path=@p", new { p = file });

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

        Log(logFile, $"=== SCAN COMPLETED {DateTime.Now} ===");
    }

    private string ComputeSHA256(string file)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(file);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }

    private void Log(string logFile, string message)
    {
        File.AppendAllText(logFile, message + "\n");
    }
}
