``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1081 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.301
  [Host]    : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT
  MediumRun : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|             Method |     Mean |    Error |   StdDev |   Median |      Min |      Max |      P90 | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|------:|------:|------:|----------:|
|                Map | 10.29 ns | 0.147 ns | 0.215 ns | 10.44 ns | 10.01 ns | 10.57 ns | 10.52 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| MapWithContextNone | 10.26 ns | 0.151 ns | 0.221 ns | 10.09 ns | 10.03 ns | 10.52 ns | 10.50 ns |  1.00 |    0.01 |     - |     - |     - |         - |
| MapWithContextHalf | 10.27 ns | 0.014 ns | 0.020 ns | 10.26 ns | 10.23 ns | 10.30 ns | 10.30 ns |  1.00 |    0.02 |     - |     - |     - |         - |
|  MapWithContextAll | 10.48 ns | 0.013 ns | 0.019 ns | 10.48 ns | 10.44 ns | 10.51 ns | 10.50 ns |  1.02 |    0.02 |     - |     - |     - |         - |
