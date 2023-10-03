``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1083 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.303
  [Host]    : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  MediumRun : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|          Method |     Mean |     Error |    StdDev |   Median |      Min |      Max |      P90 | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------- |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|------:|------:|----------:|
|       CustomInt | 1.071 ns | 0.0020 ns | 0.0028 ns | 1.071 ns | 1.067 ns | 1.078 ns | 1.074 ns |     - |     - |     - |         - |
|      GenericInt | 1.069 ns | 0.0023 ns | 0.0034 ns | 1.068 ns | 1.066 ns | 1.079 ns | 1.073 ns |     - |     - |     - |         - |
|  CustomNullable | 1.162 ns | 0.0749 ns | 0.1050 ns | 1.073 ns | 1.067 ns | 1.279 ns | 1.278 ns |     - |     - |     - |         - |
| GenericNullable | 1.074 ns | 0.0057 ns | 0.0085 ns | 1.072 ns | 1.066 ns | 1.100 ns | 1.085 ns |     - |     - |     - |         - |
|     CustomClass | 1.067 ns | 0.0011 ns | 0.0015 ns | 1.067 ns | 1.065 ns | 1.071 ns | 1.070 ns |     - |     - |     - |         - |
|    GenericClass | 1.067 ns | 0.0010 ns | 0.0015 ns | 1.067 ns | 1.065 ns | 1.071 ns | 1.069 ns |     - |     - |     - |         - |
|    CustomStruct | 1.072 ns | 0.0033 ns | 0.0047 ns | 1.071 ns | 1.068 ns | 1.087 ns | 1.079 ns |     - |     - |     - |         - |
|   GenericStruct | 1.069 ns | 0.0013 ns | 0.0020 ns | 1.069 ns | 1.066 ns | 1.073 ns | 1.071 ns |     - |     - |     - |         - |
