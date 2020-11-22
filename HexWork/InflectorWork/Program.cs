namespace InflectorWork
{
    using System;
    using System.Text;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    public static class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.MediumRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        //[Benchmark]
        //public string PascalizeOld() => Inflector.Pascalize("aaa_bbb_ccc_ddd");

        [Benchmark]
        public string UnderscoreOld() => Inflector.Underscore("aaaBbbCccDdd");

        [Benchmark]
        public string UnderscoreNew() => Inflector.Underscore2("aaaBbbCccDdd");
    }

    public static class Inflector
    {
        // TODO new version

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Underscore2(string word)
        {
            return Underscore2(word, false);
        }

        public static unsafe string Underscore2(string word, bool toUpper)
        {
            if (String.IsNullOrEmpty(word))
            {
                return word;
            }

            var bufferSize = word.Length << 1;
            var buffer = bufferSize < 2048 ? stackalloc char[bufferSize] : new char[bufferSize];
            var length = 0;

            // TODO ptr slide
            fixed (char* pBuffer = buffer)
            {
                // TODO for ?
                foreach (var c in word)
                {
                    if (Char.IsUpper(c) && (length > 0))
                    {
                        pBuffer[length++] = '_';
                    }

                    pBuffer[length++] = toUpper ? Char.ToUpperInvariant(c) : Char.ToLowerInvariant(c);
                }

                return new string(pBuffer, 0, length);
            }
        }

        // --------------------------------------------------------------------------------
        // Old
        // --------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Pascalize(string word) => Camelize(word, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Camelize(string word) => Camelize(word, false);

        public static string Camelize(string word, bool toUpper)
        {
            if (String.IsNullOrEmpty(word))
            {
                return word;
            }

            var isLowerPrevious = false;
            var sb = new StringBuilder(word.Length);
            foreach (var c in word)
            {
                if (c == '_')
                {
                    toUpper = true;
                }
                else
                {
                    if (toUpper)
                    {
                        sb.Append(Char.ToUpperInvariant(c));
                        toUpper = false;
                    }
                    else if (isLowerPrevious)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append(Char.ToLowerInvariant(c));
                    }

                    isLowerPrevious = Char.IsLower(c);
                }
            }

            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Underscore(string word)
        {
            return Underscore(word, false);
        }

        public static string Underscore(string word, bool toUpper)
        {
            if (String.IsNullOrEmpty(word))
            {
                return word;
            }

            var sb = new StringBuilder(word.Length * 2);
            foreach (var c in word)
            {
                if (Char.IsUpper(c) && (sb.Length > 0))
                {
                    sb.Append('_');
                }

                sb.Append(toUpper ? Char.ToUpperInvariant(c) : Char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }
    }
}
