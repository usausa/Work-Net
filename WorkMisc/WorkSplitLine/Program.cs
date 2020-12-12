using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace WorkSplitLine
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Columns;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    public static class Program
    {
        public static void Main()
        {
            string a = "Nickname: usausa\r\nFirstName: usa\nLastName: usa\n";
            var b = ReadLines(a);
            var lines = a.AsSpan().SplitLines();
            foreach (var line in lines)
            {
                Debug.WriteLine($"> [{new string(line)}]");
            }

            // TODO test

            //BenchmarkRunner.Run<Benchmark>();
        }

        private static List<string> ReadLines(string data)
        {
            var list = new List<string>();
            var reader = new StringReader(data);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                list.Add(line);
            }

            return list;
        }

    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
            AddColumn(
                StatisticColumn.Mean,
                StatisticColumn.Min,
                StatisticColumn.Max,
                StatisticColumn.P90,
                StatisticColumn.Error,
                StatisticColumn.StdDev);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.MediumRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private const string Data = "Nickname: usausa\r\nFirstName: usa\nLastName: usa";

        [Benchmark]
        public void Test()
        {
            foreach (var _ in Data.AsSpan().SplitLines())
            {
            }
        }
    }

    public static class SpanExtensions
    {
        public static SplitLinesEnumerator SplitLines(this ReadOnlySpan<char> value) => new(value);

        public ref struct SplitLinesEnumerator
        {
            private ReadOnlySpan<char> remain;

            public ReadOnlySpan<char> Current { get; private set; }

            public SplitLinesEnumerator(ReadOnlySpan<char> remain)
            {
                this.remain = remain;
                Current = default;
            }

            public SplitLinesEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                // No more
                if (remain.Length == 0)
                {
                    return false;
                }

                // Last
                var index = remain.IndexOfAny('\r', '\n');
                if (index == -1)
                {
                    Current = remain;
                    remain = ReadOnlySpan<char>.Empty;
                    return true;
                }

                if ((index < remain.Length - 1) && (remain[index] == '\r') && (remain[index + 1] == '\n'))
                {
                    Current = remain.Slice(0, index);
                    remain = remain.Slice(index + 2);
                    return true;
                }

                Current = remain.Slice(0, index);
                remain = remain.Slice(index + 1);
                return true;
            }
        }
    }
}
