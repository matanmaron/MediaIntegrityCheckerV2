public static class LogManager
{
    public static string CurrentLogPath;

    public static void Initialize()
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        // Cleanup: delete old log files (older than 30 days)
        foreach (var file in Directory.GetFiles(logDir, "log_*.txt"))
        {
            var info = new FileInfo(file);
            if (info.CreationTime < DateTime.Now.AddDays(-30))
                File.Delete(file);
        }

        var logFileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        CurrentLogPath = Path.Combine(logDir, logFileName);
        File.WriteAllText(CurrentLogPath, "=== RUN STARTED ===\n");
    }

    public static void Log(string message)
    {
        File.AppendAllText(CurrentLogPath, $"{DateTime.Now:u} | {message}\n");
    }
}