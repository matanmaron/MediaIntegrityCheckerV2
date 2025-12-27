public class StateService
{
    public bool Running { get; set; } = false;
    public string? ActiveLogFile { get; set; } = null;
    public string TargetPath { get; set; } = "/data"; // to be set via Docker bind
}