using Microsoft.Data.Sqlite;
using Dapper;

public class DatabaseService
{
    private readonly string _dbPath = "/data/checksums.db";

    public DatabaseService()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Execute("""
                         CREATE TABLE IF NOT EXISTS Files (
                             Path TEXT PRIMARY KEY,
                             Checksum TEXT,
                             LastSeen TEXT
                         );
                     """);
    }

    public void UpsertFile(string path, string checksum)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Execute("""
                         INSERT INTO Files(Path, Checksum, LastSeen)
                         VALUES(@path, @checksum, datetime('now'))
                         ON CONFLICT(Path) DO UPDATE SET
                         Checksum=@checksum, LastSeen=datetime('now');
                     """, new { path, checksum });
    }

    public Dictionary<string,string> LoadAll()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        return conn.Query<(string Path, string Checksum)>("SELECT Path,Checksum FROM Files")
            .ToDictionary(x => x.Path, x => x.Checksum);
    }

    public void Remove(string path)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Execute("DELETE FROM Files WHERE Path=@p", new { p = path });
    }
}