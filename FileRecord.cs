public class FileRecord
{
    public string Path { get; set; } = "";
    public string Hash { get; set; } = "";
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}