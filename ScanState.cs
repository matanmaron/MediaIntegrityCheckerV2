public class ScanState
{
    public bool IsRunning { get; set; } = false;
    public string LogFile { get; set; } = string.Empty;
    public CancellationTokenSource TokenSource { get; set; }
}