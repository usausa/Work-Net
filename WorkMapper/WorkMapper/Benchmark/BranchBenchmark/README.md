``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1237 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.401
  [Host]    : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  MediumRun : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|   Method |     Mean |     Error |    StdDev |   Median |      Min |      Max |      P90 | Allocated |
|--------- |---------:|----------:|----------:|---------:|---------:|---------:|---------:|----------:|
|  Branch0 | 1.294 ns | 0.0021 ns | 0.0030 ns | 1.294 ns | 1.288 ns | 1.299 ns | 1.297 ns |         - |
| BranchS0 | 1.294 ns | 0.0026 ns | 0.0038 ns | 1.294 ns | 1.287 ns | 1.302 ns | 1.299 ns |         - |
|  Branch1 | 1.293 ns | 0.0028 ns | 0.0042 ns | 1.292 ns | 1.287 ns | 1.300 ns | 1.300 ns |         - |
| BranchS1 | 1.190 ns | 0.0740 ns | 0.1085 ns | 1.289 ns | 1.076 ns | 1.297 ns | 1.295 ns |         - |
