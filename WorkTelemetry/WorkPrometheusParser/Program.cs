namespace WorkPrometheusParser;

using System.Diagnostics;
using System.Text.RegularExpressions;

internal class Program
{
    public static void Main()
    {
        var lines = File.ReadAllLines("result.txt");

        //var regex = new Regex(@"^(\w+)(\{[^}]+\})?\s+(\d+(\.\d+)?)\s+(\d+)?$");
        //var regex = new Regex(@"^(\w+)(\{[^}]+\})?\s+(\d+(\.\d+)?)$");
        var regex = new Regex(@"^(\w+)(\{[^}]+\})?\s+(\d+(\.\d+)?)(\s+(\d+))?$");
        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var key = match.Groups[1].Value;
            var tags = match.Groups[2].Value;
            var value = double.Parse(match.Groups[3].Value);
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

        static Dictionary<string, string> ParseTags(string tags)
        {
            var tagDict = new Dictionary<string, string>();
            var regex = new Regex(@"(\w+)=""([^""]+)""");
            var matches = regex.Matches(tags);

            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                tagDict[key] = value;
            }

            return tagDict;
        }
    }
}
