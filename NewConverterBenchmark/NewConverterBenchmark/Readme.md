|                   Method |      Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------- |----------:|----------:|----------:|-------:|------:|------:|----------:|
|          ValueToValueRaw |  2.575 ns | 0.1098 ns | 0.0060 ns |      - |     - |     - |         - |
|          ValueToClassRaw |  2.540 ns | 0.2600 ns | 0.0143 ns |      - |     - |     - |         - |
|          ClassToValueRaw |  2.549 ns | 0.6222 ns | 0.0341 ns |      - |     - |     - |         - |
|          ClassToClassRaw |  2.543 ns | 0.3420 ns | 0.0187 ns |      - |     - |     - |         - |
|     ValueToValueAsObject |  4.282 ns | 0.7869 ns | 0.0431 ns |      - |     - |     - |         - |
|     ValueToClassAsObject |  4.559 ns | 0.6557 ns | 0.0359 ns |      - |     - |     - |         - |
|     ClassToValueAsObject |  4.278 ns | 0.9548 ns | 0.0523 ns |      - |     - |     - |         - |
|     ClassToClassAsObject |  4.203 ns | 0.3326 ns | 0.0182 ns |      - |     - |     - |         - |
|         ValueToValueCast | 10.931 ns | 2.0780 ns | 0.1139 ns | 0.0115 |     - |     - |      48 B |
|         ValueToClassCast |  8.414 ns | 0.5435 ns | 0.0298 ns | 0.0057 |     - |     - |      24 B |
|         ClassToValueCast |  7.792 ns | 2.3426 ns | 0.1284 ns | 0.0057 |     - |     - |      24 B |
|         ClassToClassCast |  4.004 ns | 0.5828 ns | 0.0319 ns |      - |     - |     - |         - |
| ValueToValueAsObjectCast | 13.855 ns | 5.2706 ns | 0.2889 ns | 0.0115 |     - |     - |      48 B |
| ValueToClassAsObjectCast |  9.518 ns | 2.0065 ns | 0.1100 ns | 0.0057 |     - |     - |      24 B |
| ClassToValueAsObjectCast | 10.088 ns | 1.2368 ns | 0.0678 ns | 0.0057 |     - |     - |      24 B |
| ClassToClassAsObjectCast |  5.679 ns | 0.8338 ns | 0.0457 ns |      - |     - |     - |         - |
