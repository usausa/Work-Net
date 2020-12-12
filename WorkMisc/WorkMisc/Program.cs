namespace WorkMisc
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

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
            BenchmarkRunner.Run<Benchmark>();
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
        private const string Data = "Nickname: usausa,FirstName: usa,LastName: usa";

        //[Benchmark]
        //public void StringReader()
        //{
        //    var reader = new StringReader(Data);
        //    while ((_ = reader.ReadLine()) != null)
        //    {
        //    }
        //}

        [Benchmark]
        public void Split()
        {
            foreach (var _ in Data.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
            }
        }

        [Benchmark]
        public void Span()
        {
            foreach (var _ in Data.AsSpan().SplitLines(","))
            {
            }
        }
    }

    public static class SpanExtensions
    {
        public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> str, string splitter) => new(str, splitter);

        // TODO
        // char
        // char2
        // char array
        // string
        // string array
        // *2 ?

        public ref struct LineSplitEnumerator
        {
            private ReadOnlySpan<char> str;

            private readonly string splitter;

            // TODO prop?

            public ReadOnlySpan<char> Current { get; private set; }

            public LineSplitEnumerator(ReadOnlySpan<char> str, string splitter)
            {
                this.str = str;
                this.splitter = splitter;
                Current = default;
            }

            public LineSplitEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                // TODO
                var span = str;

                // End
                if (span.Length == 0)
                {
                    return false;
                }

                // Last
                var index = span.IndexOf(splitter);
                if (index == -1)
                {
                    str = ReadOnlySpan<char>.Empty;
                    Current = span;
                    return true;
                }

                Current = span.Slice(0, index);
                str = span.Slice(index + 1);
                return true;
            }
        }
    }

    //------------------------

    //public static class SpanExtensions
    //{
    //    public static LineSplitEnumerator SplitLines(this string str)
    //    {
    //        // LineSplitEnumerator is a struct so there is no allocation here
    //        return new(str.AsSpan());
    //    }

    //    // Must be a ref struct as it contains a ReadOnlySpan<char>
    //    public ref struct LineSplitEnumerator
    //    {
    //        private ReadOnlySpan<char> str;

    //        public LineSplitEnumerator(ReadOnlySpan<char> str)
    //        {
    //            this.str = str;
    //            Current = default;
    //        }

    //        // Needed to be compatible with the foreach operator
    //        public LineSplitEnumerator GetEnumerator() => this;

    //        public bool MoveNext()
    //        {
    //            var span = str;
    //            if (span.Length == 0) // Reach the end of the string
    //                return false;

    //            var index = span.IndexOfAny('\r', '\n');
    //            if (index == -1) // The string is composed of only one line
    //            {
    //                str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
    //                Current = new LineSplitEntry(span, ReadOnlySpan<char>.Empty);
    //                return true;
    //            }

    //            if (index < span.Length - 1 && span[index] == '\r')
    //            {
    //                // Try to consume the '\n' associated to the '\r'
    //                var next = span[index + 1];
    //                if (next == '\n')
    //                {
    //                    Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 2));
    //                    str = span.Slice(index + 2);
    //                    return true;
    //                }
    //            }

    //            Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 1));
    //            str = span.Slice(index + 1);
    //            return true;
    //        }

    //        public LineSplitEntry Current { get; private set; }
    //    }

    //    public readonly ref struct LineSplitEntry
    //    {
    //        public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
    //        {
    //            Line = line;
    //            Separator = separator;
    //        }

    //        public ReadOnlySpan<char> Line { get; }

    //        public ReadOnlySpan<char> Separator { get; }

    //        public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
    //        {
    //            line = Line;
    //            separator = Separator;
    //        }

    //        public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
    //    }
    //}
}
