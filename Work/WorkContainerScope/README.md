```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.3958/23H2/2023Update/SunValley3)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 8.0.400
  [Host]    : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  MediumRun : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method | Mean     | Error   | StdDev  | Min      | Max      | P90      | Allocated |
|------- |---------:|--------:|--------:|---------:|---------:|---------:|----------:|
| Scope1 | 445.8 ns | 4.56 ns | 6.82 ns | 437.4 ns | 460.2 ns | 455.1 ns |         - |
| Scope2 | 436.7 ns | 1.34 ns | 1.87 ns | 434.2 ns | 440.1 ns | 439.3 ns |         - |
| Scope3 | 437.9 ns | 2.06 ns | 2.89 ns | 434.2 ns | 446.3 ns | 440.9 ns |         - |
