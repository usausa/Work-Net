``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=7.0.102
  [Host]    : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  MediumRun : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                  Method | Size |      Mean |     Error |    StdDev |       Min |       Max |       P90 | Code Size | Allocated |
|------------------------ |----- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|
|         **FindClassByLoop** |    **2** |  **7.733 ns** | **0.0511 ns** | **0.0749 ns** |  **7.647 ns** |  **7.913 ns** |  **7.827 ns** |     **840 B** |         **-** |
|        FindStructByLoop |    2 |  8.025 ns | 0.0461 ns | 0.0646 ns |  7.944 ns |  8.211 ns |  8.123 ns |     863 B |         - |
|     FindStructByRefLoop |    2 |  7.716 ns | 0.0755 ns | 0.1130 ns |  7.550 ns |  7.932 ns |  7.856 ns |     842 B |         - |
| FindStructBySpanRefLoop |    2 |  7.946 ns | 0.0958 ns | 0.1404 ns |  7.812 ns |  8.395 ns |  8.106 ns |     748 B |         - |
|  FindStructBySpanRefAdd |    2 |  7.704 ns | 0.0508 ns | 0.0712 ns |  7.593 ns |  7.865 ns |  7.792 ns |     702 B |         - |
| FindStructBySpanRefAdd2 |    2 |  7.474 ns | 0.0348 ns | 0.0511 ns |  7.389 ns |  7.591 ns |  7.553 ns |     690 B |         - |
|         **FindClassByLoop** |   **16** | **52.854 ns** | **0.2320 ns** | **0.3327 ns** | **52.349 ns** | **53.573 ns** | **53.392 ns** |     **840 B** |         **-** |
|        FindStructByLoop |   16 | 53.183 ns | 0.2595 ns | 0.3803 ns | 52.624 ns | 54.043 ns | 53.700 ns |     863 B |         - |
|     FindStructByRefLoop |   16 | 53.095 ns | 0.2611 ns | 0.3908 ns | 52.645 ns | 53.942 ns | 53.739 ns |     842 B |         - |
| FindStructBySpanRefLoop |   16 | 53.093 ns | 0.1782 ns | 0.2317 ns | 52.697 ns | 53.663 ns | 53.356 ns |     748 B |         - |
|  FindStructBySpanRefAdd |   16 | 50.001 ns | 0.1890 ns | 0.2587 ns | 49.679 ns | 50.646 ns | 50.356 ns |     702 B |         - |
| FindStructBySpanRefAdd2 |   16 | 49.594 ns | 0.1656 ns | 0.2266 ns | 49.286 ns | 50.161 ns | 49.875 ns |     690 B |         - |
