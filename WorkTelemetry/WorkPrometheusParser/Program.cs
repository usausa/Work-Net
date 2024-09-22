namespace WorkPrometheusParser;

using System.Diagnostics;
using System.Text.RegularExpressions;

internal partial class Program
{
    //[GeneratedRegex(@"^(\w+)(\{[^}]+\})?\s+(\d+(\.\d+)?)(\s+(\d+))?$")]
    [GeneratedRegex(@"^(\w+)(\{([^}]+)\})?\s+(\d+(\.\d+)?|NaN)(\s+(\d+))?$")]
    private static partial Regex MetricsRegex();

    [GeneratedRegex(@"(\w+)=""([^""]+)""")]
    private static partial Regex TagRegex();

    public static void Main()
    {
        var lines = File.ReadAllLines("result.txt");

        foreach (var line in lines)
        {
            var match = MetricsRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var key = match.Groups[1].Value;
            var tags = match.Groups[2].Value;
            var value = Double.TryParse(match.Groups[3].Value, out var result) ? result : (double?)null;
            var timestamp = match.Groups[6].Success ? long.Parse(match.Groups[6].Value) : (long?)null;

            Debug.WriteLine($"{key} {value} {timestamp}");

            if (!String.IsNullOrEmpty(tags))
            {
                var dic = ParseTags(tags);
                foreach (var (k, v) in dic)
                {
                    Debug.WriteLine($"  {k}={v}");
                }
            }
        }
    }

    private static Dictionary<string, string> ParseTags(string tags)
    {
        var dictionary = new Dictionary<string, string>();

        foreach (Match match in TagRegex().Matches(tags))
        {
            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            dictionary[key] = value;
        }

        return dictionary;
    }
}
