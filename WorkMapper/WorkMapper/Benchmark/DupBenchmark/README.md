``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1083 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.302
  [Host]    : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT
  MediumRun : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|       Method |     Mean |     Error |    StdDev |      Min |      Max |      P90 |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |---------:|----------:|----------:|---------:|---------:|---------:|-------:|------:|------:|----------:|
|   DupFactory | 2.779 ns | 0.0459 ns | 0.0687 ns | 2.721 ns | 2.969 ns | 2.882 ns | 0.0014 |     - |     - |      24 B |
| LocalFactory | 2.836 ns | 0.0402 ns | 0.0601 ns | 2.730 ns | 2.973 ns | 2.922 ns | 0.0014 |     - |     - |      24 B |
