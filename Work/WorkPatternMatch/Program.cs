namespace WorkPatternMatch;

using System.Diagnostics;

internal class Program
{
    static void Main()
    {
        Debug.WriteLine(PatternMatcher.IsMatch("ab?.txt", "abc.txt"));
        Debug.WriteLine(PatternMatcher.IsMatch("a*.txt", "abc.txt"));
        Debug.WriteLine(PatternMatcher.IsMatch("*.txt", "abc.txt"));
        Debug.WriteLine(PatternMatcher.IsMatch("a?g.txt", "abcdefg.txt"));
        Debug.WriteLine(PatternMatcher.IsMatch("a*g.txt", "abcdefg.txt"));
        Debug.WriteLine(PatternMatcher.IsMatch("a*b*g.txt", "abcdefg.txt"));
        Debug.WriteLine(PatternMatcher.IsMatch("*efg.txt", "abcdefg.txt"));
    }
}

public class PatternMatcher
{
    public static bool IsMatch(ReadOnlySpan<char> pattern, ReadOnlySpan<char> target)
    {
        var i = 0;
        var j = 0;
        var start = -1;
        var match = 0;

        while (j < target.Length)
        {
            if ((i < pattern.Length) && ((pattern[i] == '?') || (pattern[i] == target[j])))
            {
                i++;
                j++;
            }
            else if ((i < pattern.Length) && (pattern[i] == '*'))
            {
                start = i;
                match = j;
                i++;
            }
            else if (start != -1)
            {
                i = start + 1;
                match++;
                j = match;
            }
            else
            {
                return false;
            }
        }

        while ((i < pattern.Length) && (pattern[i] == '*'))
        {
            i++;
        }

        return i == pattern.Length;
    }
}
