``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=7.0.102
  [Host]    : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  MediumRun : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|        Method | Size |      Mean |     Error |    StdDev |    Median |       Min |       Max |       P90 | Code Size | Allocated |
|-------------- |----- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|
| **OperatorEqual** |    **2** | **0.9944 ns** | **0.0058 ns** | **0.0083 ns** | **0.9938 ns** | **0.9813 ns** | **1.0153 ns** | **1.0049 ns** |     **363 B** |         **-** |
| EqualsDefault |    2 | 0.9977 ns | 0.0241 ns | 0.0330 ns | 0.9953 ns | 0.9544 ns | 1.0821 ns | 1.0449 ns |     363 B |         - |
| EqualsOrdinal |    2 | 2.2201 ns | 0.0123 ns | 0.0180 ns | 2.2209 ns | 2.1951 ns | 2.2660 ns | 2.2372 ns |     763 B |         - |
|  SpanSequence |    2 | 1.4113 ns | 0.0058 ns | 0.0080 ns | 1.4101 ns | 1.3985 ns | 1.4351 ns | 1.4195 ns |     391 B |         - |
|        Custom |    2 | 1.7539 ns | 0.0475 ns | 0.0696 ns | 1.7424 ns | 1.6565 ns | 1.8992 ns | 1.8604 ns |     165 B |         - |
| **OperatorEqual** |    **4** | **1.2494 ns** | **0.0106 ns** | **0.0152 ns** | **1.2484 ns** | **1.2181 ns** | **1.2875 ns** | **1.2664 ns** |     **363 B** |         **-** |
| EqualsDefault |    4 | 1.2331 ns | 0.0101 ns | 0.0138 ns | 1.2315 ns | 1.2130 ns | 1.2733 ns | 1.2500 ns |     363 B |         - |
| EqualsOrdinal |    4 | 2.2324 ns | 0.0138 ns | 0.0197 ns | 2.2318 ns | 2.2019 ns | 2.2647 ns | 2.2591 ns |     763 B |         - |
|  SpanSequence |    4 | 1.6374 ns | 0.0128 ns | 0.0183 ns | 1.6392 ns | 1.6059 ns | 1.6769 ns | 1.6588 ns |     391 B |         - |
|        Custom |    4 | 0.7730 ns | 0.0258 ns | 0.0387 ns | 0.7786 ns | 0.6923 ns | 0.8461 ns | 0.8210 ns |     165 B |         - |
| **OperatorEqual** |    **8** | **1.3531 ns** | **0.0242 ns** | **0.0355 ns** | **1.3587 ns** | **1.2632 ns** | **1.4363 ns** | **1.3900 ns** |     **363 B** |         **-** |
| EqualsDefault |    8 | 1.3325 ns | 0.0291 ns | 0.0435 ns | 1.3225 ns | 1.2700 ns | 1.4425 ns | 1.3877 ns |     363 B |         - |
| EqualsOrdinal |    8 | 2.4367 ns | 0.0186 ns | 0.0255 ns | 2.4311 ns | 2.4041 ns | 2.5061 ns | 2.4700 ns |     763 B |         - |
|  SpanSequence |    8 | 1.6722 ns | 0.0119 ns | 0.0171 ns | 1.6706 ns | 1.6430 ns | 1.7089 ns | 1.6977 ns |     391 B |         - |
|        Custom |    8 | 1.2373 ns | 0.0143 ns | 0.0206 ns | 1.2304 ns | 1.2087 ns | 1.2830 ns | 1.2646 ns |     165 B |         - |
| **OperatorEqual** |   **16** | **1.2668 ns** | **0.0217 ns** | **0.0311 ns** | **1.2654 ns** | **1.2127 ns** | **1.3357 ns** | **1.3148 ns** |     **363 B** |         **-** |
| EqualsDefault |   16 | 1.2428 ns | 0.0135 ns | 0.0185 ns | 1.2437 ns | 1.2085 ns | 1.2758 ns | 1.2663 ns |     363 B |         - |
| EqualsOrdinal |   16 | 2.2951 ns | 0.0210 ns | 0.0308 ns | 2.3034 ns | 2.2513 ns | 2.3604 ns | 2.3373 ns |     763 B |         - |
|  SpanSequence |   16 | 1.7313 ns | 0.0118 ns | 0.0166 ns | 1.7298 ns | 1.6995 ns | 1.7613 ns | 1.7536 ns |     391 B |         - |
|        Custom |   16 | 1.9794 ns | 0.1458 ns | 0.2138 ns | 1.8505 ns | 1.7139 ns | 2.2266 ns | 2.2050 ns |     165 B |         - |
