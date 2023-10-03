``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1237 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.401
  [Host]   : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  ShortRun : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
|  Method |     Mean |     Error |    StdDev |      Min |      Max |      P90 |  Gen 0 | Allocated |
|-------- |---------:|----------:|----------:|---------:|---------:|---------:|-------:|----------:|
|   Short | 6.696 ns | 0.4350 ns | 0.0238 ns | 6.669 ns | 6.713 ns | 6.712 ns | 0.0038 |      64 B |
| Default | 7.800 ns | 0.2052 ns | 0.0112 ns | 7.791 ns | 7.813 ns | 7.810 ns | 0.0038 |      64 B |
