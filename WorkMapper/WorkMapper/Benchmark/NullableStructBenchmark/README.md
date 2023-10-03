``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1083 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.302
  [Host]    : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT
  MediumRun : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method |      Mean |     Error |    StdDev |    Median |       Min |       Max |       P90 | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|------:|------:|----------:|
|  Map1A |  1.722 ns | 0.0030 ns | 0.0045 ns |  1.722 ns |  1.716 ns |  1.734 ns |  1.729 ns |     - |     - |     - |         - |
|  Map1B |  1.721 ns | 0.0060 ns | 0.0087 ns |  1.718 ns |  1.715 ns |  1.746 ns |  1.733 ns |     - |     - |     - |         - |
|  Map2A | 13.602 ns | 0.5909 ns | 0.8475 ns | 13.618 ns | 12.729 ns | 14.492 ns | 14.455 ns |     - |     - |     - |         - |
|  Map2B | 13.633 ns | 0.5748 ns | 0.8059 ns | 14.376 ns | 12.775 ns | 14.442 ns | 14.410 ns |     - |     - |     - |         - |
|  Map4A | 19.676 ns | 0.1635 ns | 0.2292 ns | 19.642 ns | 19.392 ns | 20.008 ns | 19.917 ns |     - |     - |     - |         - |
|  Map4B | 19.938 ns | 0.0421 ns | 0.0590 ns | 19.922 ns | 19.870 ns | 20.079 ns | 20.018 ns |     - |     - |     - |         - |
|  Map6A | 28.055 ns | 0.0449 ns | 0.0659 ns | 28.051 ns | 27.923 ns | 28.219 ns | 28.136 ns |     - |     - |     - |         - |
|  Map6B | 24.333 ns | 0.1385 ns | 0.2030 ns | 24.233 ns | 24.067 ns | 24.652 ns | 24.580 ns |     - |     - |     - |         - |
|  Map8A | 29.317 ns | 0.3968 ns | 0.5691 ns | 29.396 ns | 28.533 ns | 30.121 ns | 29.953 ns |     - |     - |     - |         - |
|  Map8B | 26.215 ns | 0.0747 ns | 0.1095 ns | 26.193 ns | 25.997 ns | 26.547 ns | 26.330 ns |     - |     - |     - |         - |
