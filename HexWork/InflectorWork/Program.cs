namespace InflectorWork
{
    using System;
    using System.Text;
    using System.Runtime.CompilerServices;

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
        [Benchmark]
        public string PascalizeOld() => Inflector.Pascalize("aaa_bbb_ccc_ddd");

        [Benchmark]
        public string PascalizeNew() => Inflector.Pascalize2("aaa_bbb_ccc_ddd");

        [Benchmark]
        public string UnderscoreOld() => Inflector.Underscore("aaaBbbCccDdd");

        [Benchmark]
        public string UnderscoreNew() => Inflector.Underscore2("aaaBbbCccDdd");

        [Benchmark]
        public string UnderscoreNewB() => Inflector.Underscore3("aaaBbbCccDdd");
    }

    public static class Inflector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Pascalize2(string word) => Camelize2(word, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Camelize2(string word) => Camelize2(word, false);

        public static unsafe string Camelize2(string word, bool toUpper)
        {
            if ((word is null) || (word.Length == 0))
            {
                return word;
            }

            var buffer = word.Length < 2048 ? stackalloc char[word.Length] : new char[word.Length];
            var length = 0;

            fixed (char* pBuffer = buffer)
            {
                var isLowerPrevious = false;
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
                            pBuffer[length++] = Char.ToUpperInvariant(c);
                            toUpper = false;
                        }
                        else if (isLowerPrevious)
                        {
                            pBuffer[length++] = c;
                        }
                        else
                        {
                            pBuffer[length++] = Char.ToLowerInvariant(c);
                        }

                        isLowerPrevious = Char.IsLower(c);
                    }
                }

                return new string(pBuffer, 0, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Underscore2(string word)
        {
            return Underscore2(word, false);
        }

        public static unsafe string Underscore2(string word, bool toUpper)
        {
            if ((word is null) || (word.Length == 0))
            {
                return word;
            }

            var bufferSize = word.Length << 1;
            var buffer = bufferSize < 2048 ? stackalloc char[bufferSize] : new char[bufferSize];
            var length = 0;

            fixed (char* pBuffer = buffer)
            {
                if (toUpper)
                {
                    foreach (var c in word)
                    {
                        if (Char.IsUpper(c))
                        {
                            if (length > 0)
                            {
                                pBuffer[length++] = '_';
                            }

                            pBuffer[length++] = c;
                        }
                        else
                        {
                            pBuffer[length++] = Char.ToUpperInvariant(c);
                        }
                    }
                }
                else
                {
                    foreach (var c in word)
                    {
                        if (Char.IsUpper(c))
                        {
                            if (length > 0)
                            {
                                pBuffer[length++] = '_';
                            }

                            pBuffer[length++] = Char.ToLowerInvariant(c);
                        }
                        else
                        {
                            pBuffer[length++] = c;
                        }
                    }
                }

                return new string(pBuffer, 0, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Underscore3(string word)
        {
            return Underscore3(word, false);
        }

        public static unsafe string Underscore3(string word, bool toUpper)
        {
            if ((word is null) || (word.Length == 0))
            {
                return word;
            }

            var bufferSize = word.Length << 1;
            var buffer = bufferSize < 2048 ? stackalloc char[bufferSize] : new char[bufferSize];
            var length = 0;

            fixed (char* pBuffer = buffer)
            {
                if (toUpper)
                {
                    foreach (var c in word)
                    {
                        if (IsUpper(c))
                        {
                            if (length > 0)
                            {
                                pBuffer[length++] = '_';
                            }

                            pBuffer[length++] = c;
                        }
                        else
                        {
                            pBuffer[length++] = ToUpper(c);
                        }
                    }
                }
                else
                {
                    foreach (var c in word)
                    {
                        if (IsUpper(c))
                        {
                            if (length > 0)
                            {
                                pBuffer[length++] = '_';
                            }

                            pBuffer[length++] = ToLower(c);
                        }
                        else
                        {
                            pBuffer[length++] = c;
                        }
                    }
                }

                return new string(pBuffer, 0, length);
            }
        }

        public const int Offset = 'A' - 'a';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUpper(char c) => (c >= 'A') && (c >= 'Z');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToLower(char c) => (char)(c - Offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToUpper(char c) => (char)(c + Offset);

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
