``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1081 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.301
  [Host]    : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT
  MediumRun : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|               Method |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
|    TypedClassToClass | 1.742 ns | 0.0031 ns | 0.0046 ns |      - |     - |     - |         - |
|   TypedClassToStruct | 1.734 ns | 0.0015 ns | 0.0020 ns |      - |     - |     - |         - |
|  TypedStructToStruct | 1.732 ns | 0.0024 ns | 0.0036 ns |      - |     - |     - |         - |
|   TypedStructToClass | 1.732 ns | 0.0017 ns | 0.0023 ns |      - |     - |     - |         - |
|   ObjectClassToClass | 2.166 ns | 0.0030 ns | 0.0044 ns |      - |     - |     - |         - |
|  ObjectClassToStruct | 4.506 ns | 0.0152 ns | 0.0222 ns | 0.0014 |     - |     - |      24 B |
| ObjectStructToStruct | 6.171 ns | 0.0210 ns | 0.0314 ns | 0.0029 |     - |     - |      48 B |
|  ObjectStructToClass | 4.045 ns | 0.0088 ns | 0.0132 ns | 0.0014 |     - |     - |      24 B |
