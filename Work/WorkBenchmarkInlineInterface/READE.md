``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=7.0.102
  [Host]    : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  MediumRun : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|   Method |     Mean |     Error |    StdDev |      Min |      Max |      P90 | Allocated |
|--------- |---------:|----------:|----------:|---------:|---------:|---------:|----------:|
|   Inline | 1.078 μs | 0.0029 μs | 0.0042 μs | 1.070 μs | 1.085 μs | 1.083 μs |         - |
| NoInline | 1.080 μs | 0.0055 μs | 0.0080 μs | 1.070 μs | 1.103 μs | 1.091 μs |         - |
