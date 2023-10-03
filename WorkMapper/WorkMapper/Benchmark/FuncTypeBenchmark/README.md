``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1081 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.301
  [Host]    : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT
  MediumRun : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|       Method |     Mean |     Error |    StdDev |   Median |      Min |      Max |      P90 | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|------:|------:|----------:|
|   ByFunction | 1.290 ns | 0.0015 ns | 0.0022 ns | 1.289 ns | 1.285 ns | 1.295 ns | 1.292 ns |     - |     - |     - |         - |
| ByExpression | 1.175 ns | 0.0727 ns | 0.1066 ns | 1.078 ns | 1.071 ns | 1.288 ns | 1.285 ns |     - |     - |     - |         - |
|       ByEmit | 1.073 ns | 0.0013 ns | 0.0018 ns | 1.073 ns | 1.070 ns | 1.078 ns | 1.076 ns |     - |     - |     - |         - |
