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
            TestTextReader();
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

        // TODO split

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
