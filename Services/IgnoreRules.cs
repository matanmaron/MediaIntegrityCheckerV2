public class IgnoreRules
{
    private readonly string _rulesPath = "/config/ignore.rules";
    private HashSet<string> _patterns = new();

    public IgnoreRules()
    {
        if (File.Exists(_rulesPath))
            _patterns = File.ReadAllLines(_rulesPath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool ShouldIgnore(string path)
    {
        var file = Path.GetFileName(path);
        return _patterns.Any(p => MatchesPattern(file, p));
    }

    private bool MatchesPattern(string file, string pattern)
    {
        if (pattern.Contains('*'))
        {
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(file, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return file.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}