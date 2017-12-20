using System;

namespace WorkSplit
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public static class Program
    {
        public static void Main(string[] args)
        {
            TestSplit();
        }

        private static void TestSplit()
        {
            Debug.WriteLine("--");

            var str = "テスト,データ,,だよもん,,";
            foreach (var token in str.SplitAsEnumerable(','))
            {
                Debug.WriteLine(token);
            }

            Debug.WriteLine("--");
        }

        private static void TestTextReader()
        {
            Debug.WriteLine("--");

            var str = "テスト\nデータ\n\nだよもん\n";
            using (var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(str)), Encoding.UTF8))
            {
                foreach (var line in reader.AsEnumerable())
                {
                    Debug.WriteLine(line);
                }
            }

            Debug.WriteLine("--");
        }
    }

    public static class SplitExtensions
    {
        // TODO bytes chop?/block?*

        public static IEnumerable<string> SplitAsEnumerable(this string source, char separator)
        {
            var start = 0;
            while (true)
            {
                var index = source.IndexOf(separator, start);
                if (index == -1)
                {
                    yield return source.Substring(start);
                    break;
                }

                yield return source.Substring(start, index - start);
                start = index + 1;
            }
        }

        public static IEnumerable<string> SplitAsEnumerable(this string source, char[] separators)
        {
            var start = 0;
            while (true)
            {
                var index = source.IndexOfAny(separators, start);
                if (index == -1)
                {
                    yield return source.Substring(start);
                    break;
                }

                yield return source.Substring(start, index - start);
                start = index + 1;
            }
        }

        public static IEnumerable<string> SplitAsEnumerable(this string source, string separator)
        {
            var start = 0;
            while (true)
            {
                var index = source.IndexOf(separator, start, StringComparison.Ordinal);
                if (index == -1)
                {
                    yield return source.Substring(start);
                    break;
                }

                yield return source.Substring(start, index - start);
                start = index + 1;
            }
        }

        public static IEnumerable<string> SplitAsEnumerable(this string source, string separator, StringComparison comparisonType)
        {
            var start = 0;
            while (true)
            {
                var index = source.IndexOf(separator, start, comparisonType);
                if (index == -1)
                {
                    yield return source.Substring(start);
                    break;
                }

                yield return source.Substring(start, index - start);
                start = index + 1;
            }
        }

        public static IEnumerable<string> AsEnumerable(this TextReader reader)
        {
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                yield return line;
            }
        }
    }
}
