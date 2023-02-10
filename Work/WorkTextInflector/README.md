``` ini

BenchmarkDotNet=v0.13.4, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=7.0.102
  [Host]    : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  MediumRun : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|          Method |     Mean |    Error |   StdDev |      Min |      Max |      P90 |   Gen0 | Allocated |
|---------------- |---------:|---------:|---------:|---------:|---------:|---------:|-------:|----------:|
|       Pascalize | 14.37 μs | 0.165 μs | 0.247 μs | 14.01 μs | 14.81 μs | 14.72 μs | 2.3804 |  39.06 KB |
|        Camelize | 14.08 μs | 0.094 μs | 0.131 μs | 13.86 μs | 14.39 μs | 14.24 μs | 2.3804 |  39.06 KB |
|      Underscore | 16.32 μs | 0.085 μs | 0.124 μs | 16.06 μs | 16.60 μs | 16.50 μs | 2.3804 |  39.06 KB |
| UnderscoreUpper | 16.93 μs | 0.116 μs | 0.166 μs | 16.67 μs | 17.33 μs | 17.10 μs | 2.3804 |  39.06 KB |
