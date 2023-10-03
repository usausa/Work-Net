``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1083 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.302
  [Host]    : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT
  MediumRun : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                      Method |      Mean |     Error |    StdDev |    Median |       Min |       Max |       P90 |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|-------:|------:|------:|----------:|
|            SimpleAutoMapper | 67.068 ns | 0.2326 ns | 0.3410 ns | 67.011 ns | 66.505 ns | 67.765 ns | 67.576 ns | 0.0038 |     - |     - |      64 B |
|           SimpleAutoMapper2 | 66.846 ns | 0.8999 ns | 1.2615 ns | 67.746 ns | 65.295 ns | 68.335 ns | 68.161 ns | 0.0038 |     - |     - |      64 B |
|            SimpleTinyMapper | 28.128 ns | 0.0486 ns | 0.0681 ns | 28.101 ns | 28.058 ns | 28.308 ns | 28.208 ns | 0.0038 |     - |     - |      64 B |
|         SimpleInstantMapper | 89.730 ns | 0.2362 ns | 0.3388 ns | 89.673 ns | 89.156 ns | 90.366 ns | 90.159 ns | 0.0095 |     - |     - |     160 B |
|             SimpleRawMapper | 34.489 ns | 0.1235 ns | 0.1811 ns | 34.511 ns | 34.209 ns | 34.855 ns | 34.692 ns | 0.0038 |     - |     - |      64 B |
|           SimpleSmartMapper | 11.521 ns | 0.1184 ns | 0.1698 ns | 11.407 ns | 11.336 ns | 11.766 ns | 11.730 ns | 0.0038 |     - |     - |      64 B |
| SimpleInstantMapperWoLookup | 80.903 ns | 0.1969 ns | 0.2886 ns | 80.875 ns | 80.393 ns | 81.463 ns | 81.317 ns | 0.0095 |     - |     - |     160 B |
|     SimpleRawMapperWoLookup | 24.086 ns | 0.7118 ns | 1.0434 ns | 23.268 ns | 22.984 ns | 25.343 ns | 25.204 ns | 0.0038 |     - |     - |      64 B |
|   SimpleSmartMapperWoLookup |  8.356 ns | 0.0135 ns | 0.0203 ns |  8.351 ns |  8.323 ns |  8.400 ns |  8.382 ns | 0.0038 |     - |     - |      64 B |
|                  SimpleHand |  7.230 ns | 0.0082 ns | 0.0115 ns |  7.231 ns |  7.204 ns |  7.252 ns |  7.244 ns | 0.0038 |     - |     - |      64 B |
|             MixedAutoMapper | 63.723 ns | 0.1456 ns | 0.2088 ns | 63.780 ns | 63.314 ns | 64.083 ns | 63.962 ns | 0.0038 |     - |     - |      64 B |
|             MixedTinyMapper | 41.895 ns | 0.1385 ns | 0.2031 ns | 41.876 ns | 41.598 ns | 42.359 ns | 42.185 ns | 0.0067 |     - |     - |     112 B |
|          MixedInstantMapper | 78.865 ns | 0.1857 ns | 0.2542 ns | 78.882 ns | 78.142 ns | 79.322 ns | 79.112 ns | 0.0123 |     - |     - |     208 B |
|              MixedRawMapper | 30.731 ns | 0.0486 ns | 0.0697 ns | 30.717 ns | 30.597 ns | 30.864 ns | 30.855 ns | 0.0038 |     - |     - |      64 B |
|            MixedSmartMapper |  8.153 ns | 0.0289 ns | 0.0414 ns |  8.156 ns |  8.082 ns |  8.238 ns |  8.201 ns | 0.0038 |     - |     - |      64 B |
