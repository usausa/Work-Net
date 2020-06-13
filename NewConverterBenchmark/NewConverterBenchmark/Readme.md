|          Method |      Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------- |----------:|----------:|----------:|-------:|------:|------:|----------:|
| ValueToValueRaw |  2.565 ns | 0.3158 ns | 0.0173 ns |      - |     - |     - |         - |
| ValueToClassRaw |  2.546 ns | 0.2373 ns | 0.0130 ns |      - |     - |     - |         - |
| ClassToValueRaw |  2.563 ns | 0.0746 ns | 0.0041 ns |      - |     - |     - |         - |
| ClassToClassRaw |  2.576 ns | 0.4225 ns | 0.0232 ns |      - |     - |     - |         - |
|    ValueToValue | 10.614 ns | 3.0988 ns | 0.1699 ns | 0.0115 |     - |     - |      48 B |
|    ValueToClass |  7.361 ns | 1.4584 ns | 0.0799 ns | 0.0057 |     - |     - |      24 B |
|    ClassToValue |  7.196 ns | 1.1081 ns | 0.0607 ns | 0.0057 |     - |     - |      24 B |
|    ClassToClass |  4.224 ns | 0.2952 ns | 0.0162 ns |      - |     - |     - |         - |
