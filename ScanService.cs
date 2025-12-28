using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

public class ScanService
{
    private readonly string _scanPath;

    public ScanService(string scanPath)
    {
        _scanPath = scanPath;
    }

    public async Task RunChecksumScan(CancellationToken token, string logFile)
    {
        var startTime = DateTime.Now;
        int newCount = 0, skippedCount = 0, badCount = 0, okCount = 0, totalCount = 0;

        Directory.CreateDirectory(Path.GetDirectoryName(logFile));

        // Load existing checksums
        var dbFile = Path.Combine("data", "checksums.db");
        Directory.CreateDirectory("data");
        var checksums = new Dictionary<string, string>();
        if (File.Exists(dbFile))
        {
            foreach (var line in File.ReadAllLines(dbFile))
            {
                var parts = line.Split('|');
                if (parts.Length == 2) checksums[parts[0]] = parts[1];
            }
        }

        try
        {
            foreach (var file in SafeEnumerateFiles(_scanPath, "*.*", SearchOption.AllDirectories))
            {
                if (token.IsCancellationRequested) break;

                totalCount++;
                string name = Path.GetFileName(file);

                // Skip system / shortcut / thumbs files
                if (name.StartsWith("~") || name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    byte[] data = File.ReadAllBytes(file);
                    string hash = ComputeHash(data);

                    if (!checksums.TryGetValue(file, out var existing))
                    {
                        checksums[file] = hash;
                        File.AppendAllText(dbFile, $"{file}|{hash}{Environment.NewLine}");
                        LogLine(logFile, $"[NEW] {file}", "green");
                        newCount++;
                    }
                    else if (existing != hash)
                    {
                        checksums[file] = hash;
                        File.AppendAllText(dbFile, $"{file}|{hash}{Environment.NewLine}");
                        LogLine(logFile, $"[BAD] {file}", "red");
                        badCount++;
                    }
                    else
                    {
                        LogLine(logFile, $"[OK] {file}", "white");
                        okCount++;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    LogLine(logFile, $"[LOCK] {file} (access denied)", "yellow");
                    skippedCount++;
                }
                catch (IOException ioEx)
                {
                    LogLine(logFile, $"[LOCK] {file} ({ioEx.Message})", "yellow");
                    skippedCount++;
                }
                catch (Exception ex)
                {
                    LogLine(logFile, $"[ERROR] {file} ({ex.GetType().Name}: {ex.Message})", "red");
                    skippedCount++;
                }
            }
        }
        catch (Exception ex)
        {
            // Only unexpected errors
            LogLine(logFile, $"[ERROR] Scan failed: {ex.GetType().Name} - {ex.Message}", "red");
        }
        finally
        {
            var duration = DateTime.Now - startTime;
            var summaryLine = $"Finished scan in {duration.TotalSeconds:N1}s: " +
                              $"<span class='green'>{newCount}</span>/" +
                              $"<span class='yellow'>{skippedCount}</span>/" +
                              $"<span class='red'>{badCount}</span> found in {totalCount} files";

            LogLine(logFile, summaryLine, "white");
        }
    }

    private IEnumerable<string> SafeEnumerateFiles(string path, string searchPattern, SearchOption option)
    {
        var dirs = new Stack<string>();
        dirs.Push(path);

        while (dirs.Count > 0)
        {
            var currentDir = dirs.Pop();
            IEnumerable<string> subDirs = new List<string>();

            // Get files
            string[] files = Array.Empty<string>();
            try { files = Directory.GetFiles(currentDir, searchPattern); } 
            catch { /* skip inaccessible directories */ }
            foreach (var file in files) yield return file;

            // Get subdirectories
            try { subDirs = Directory.GetDirectories(currentDir); } catch { continue; }
            foreach (var dir in subDirs)
            {
                // Skip system folders like "System Volume Information" or hidden
                var dirName = Path.GetFileName(dir);
                if (dirName.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase)) continue;
                dirs.Push(dir);
            }
        }
    }

    private void LogLine(string logFile, string line, string color)
    {
        try { File.AppendAllText(logFile, line + Environment.NewLine); }
        catch { /* ignore logging failures */ }
    }

    private string ComputeHash(byte[] data)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(data));
    }
}
