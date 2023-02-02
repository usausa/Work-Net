``` ini

BenchmarkDotNet=v0.13.4, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=7.0.102
  [Host]    : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  MediumRun : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                    Method | Size |       Mean |     Error |    StdDev |     Median |        Min |        Max |        P90 | Code Size | Allocated |
|-------------------------- |----- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|----------:|----------:|
|           **FindClassByLoop** |    **2** |   **8.700 ns** | **0.1385 ns** | **0.2029 ns** |   **8.695 ns** |   **8.389 ns** |   **9.003 ns** |   **8.976 ns** |     **858 B** |         **-** |
|          FindStructByLoop |    2 |   8.750 ns | 0.0919 ns | 0.1375 ns |   8.731 ns |   8.551 ns |   8.997 ns |   8.936 ns |     881 B |         - |
|       FindStructByRefLoop |    2 |   8.614 ns | 0.0424 ns | 0.0634 ns |   8.623 ns |   8.538 ns |   8.734 ns |   8.694 ns |     860 B |         - |
|   FindStructBySpanRefLoop |    2 |   8.952 ns | 0.4969 ns | 0.7437 ns |   8.639 ns |   8.578 ns |  10.651 ns |  10.565 ns |     766 B |         - |
|    FindStructBySpanRefAdd |    2 |   7.966 ns | 0.0481 ns | 0.0704 ns |   7.932 ns |   7.889 ns |   8.150 ns |   8.039 ns |     720 B |         - |
|   FindStructBySpanRefAdd2 |    2 |   7.913 ns | 0.0835 ns | 0.1249 ns |   7.915 ns |   7.730 ns |   8.168 ns |   8.037 ns |     708 B |         - |
|  FindStructBySpanRefWhile |    2 |   7.977 ns | 0.0298 ns | 0.0427 ns |   7.959 ns |   7.935 ns |   8.095 ns |   8.026 ns |     719 B |         - |
| FindStructBySpanRefWhile2 |    2 |   7.961 ns | 0.0235 ns | 0.0338 ns |   7.954 ns |   7.917 ns |   8.054 ns |   8.001 ns |     721 B |         - |
|           **FindClassByLoop** |    **4** |  **15.158 ns** | **0.1363 ns** | **0.1954 ns** |  **15.157 ns** |  **14.928 ns** |  **15.544 ns** |  **15.424 ns** |     **858 B** |         **-** |
|          FindStructByLoop |    4 |  15.330 ns | 0.1608 ns | 0.2357 ns |  15.260 ns |  15.009 ns |  15.906 ns |  15.743 ns |     881 B |         - |
|       FindStructByRefLoop |    4 |  15.162 ns | 0.0829 ns | 0.1215 ns |  15.173 ns |  15.025 ns |  15.446 ns |  15.311 ns |     860 B |         - |
|   FindStructBySpanRefLoop |    4 |  15.714 ns | 0.0513 ns | 0.0768 ns |  15.714 ns |  15.605 ns |  15.918 ns |  15.802 ns |     766 B |         - |
|    FindStructBySpanRefAdd |    4 |  14.868 ns | 0.0413 ns | 0.0605 ns |  14.860 ns |  14.787 ns |  15.011 ns |  14.941 ns |     720 B |         - |
|   FindStructBySpanRefAdd2 |    4 |  14.816 ns | 0.0215 ns | 0.0287 ns |  14.810 ns |  14.774 ns |  14.900 ns |  14.840 ns |     708 B |         - |
|  FindStructBySpanRefWhile |    4 |  14.839 ns | 0.0631 ns | 0.0944 ns |  14.868 ns |  14.723 ns |  15.045 ns |  14.964 ns |     719 B |         - |
| FindStructBySpanRefWhile2 |    4 |  14.806 ns | 0.0507 ns | 0.0743 ns |  14.771 ns |  14.727 ns |  14.983 ns |  14.906 ns |     721 B |         - |
|           **FindClassByLoop** |    **8** |  **29.239 ns** | **0.2038 ns** | **0.2988 ns** |  **29.055 ns** |  **28.921 ns** |  **29.775 ns** |  **29.631 ns** |     **858 B** |         **-** |
|          FindStructByLoop |    8 |  27.973 ns | 0.0983 ns | 0.1471 ns |  27.963 ns |  27.732 ns |  28.233 ns |  28.191 ns |     881 B |         - |
|       FindStructByRefLoop |    8 |  28.967 ns | 0.0487 ns | 0.0650 ns |  28.950 ns |  28.892 ns |  29.147 ns |  29.052 ns |     860 B |         - |
|   FindStructBySpanRefLoop |    8 |  29.672 ns | 0.1583 ns | 0.2219 ns |  29.683 ns |  29.132 ns |  30.089 ns |  29.864 ns |     766 B |         - |
|    FindStructBySpanRefAdd |    8 |  28.054 ns | 0.1528 ns | 0.2191 ns |  28.088 ns |  27.611 ns |  28.479 ns |  28.281 ns |     720 B |         - |
|   FindStructBySpanRefAdd2 |    8 |  28.369 ns | 0.0747 ns | 0.1095 ns |  28.327 ns |  28.244 ns |  28.686 ns |  28.513 ns |     708 B |         - |
|  FindStructBySpanRefWhile |    8 |  28.301 ns | 0.0384 ns | 0.0539 ns |  28.289 ns |  28.234 ns |  28.442 ns |  28.385 ns |     719 B |         - |
| FindStructBySpanRefWhile2 |    8 |  27.782 ns | 0.1087 ns | 0.1594 ns |  27.814 ns |  27.515 ns |  28.065 ns |  27.946 ns |     721 B |         - |
|           **FindClassByLoop** |   **16** |  **56.904 ns** | **0.1899 ns** | **0.2842 ns** |  **56.842 ns** |  **56.547 ns** |  **57.557 ns** |  **57.220 ns** |     **858 B** |         **-** |
|          FindStructByLoop |   16 |  53.821 ns | 0.1999 ns | 0.2930 ns |  53.814 ns |  53.347 ns |  54.469 ns |  54.147 ns |     881 B |         - |
|       FindStructByRefLoop |   16 |  57.078 ns | 0.2379 ns | 0.3412 ns |  57.116 ns |  56.447 ns |  57.718 ns |  57.461 ns |     860 B |         - |
|   FindStructBySpanRefLoop |   16 |  50.676 ns | 0.2542 ns | 0.3646 ns |  50.601 ns |  50.174 ns |  51.504 ns |  51.213 ns |     766 B |         - |
|    FindStructBySpanRefAdd |   16 |  52.967 ns | 1.4877 ns | 2.2267 ns |  52.671 ns |  50.516 ns |  56.433 ns |  55.605 ns |     720 B |         - |
|   FindStructBySpanRefAdd2 |   16 |  53.926 ns | 0.1160 ns | 0.1736 ns |  53.923 ns |  53.639 ns |  54.408 ns |  54.117 ns |     708 B |         - |
|  FindStructBySpanRefWhile |   16 |  54.289 ns | 0.2607 ns | 0.3902 ns |  54.250 ns |  53.666 ns |  54.892 ns |  54.755 ns |     719 B |         - |
| FindStructBySpanRefWhile2 |   16 |  54.183 ns | 0.2299 ns | 0.3370 ns |  54.186 ns |  53.678 ns |  54.806 ns |  54.598 ns |     721 B |         - |
|           **FindClassByLoop** |   **32** |  **99.068 ns** | **0.4072 ns** | **0.5708 ns** |  **99.096 ns** |  **98.212 ns** | **100.739 ns** |  **99.651 ns** |     **858 B** |         **-** |
|          FindStructByLoop |   32 | 106.036 ns | 1.2090 ns | 1.7721 ns | 105.246 ns | 104.389 ns | 110.754 ns | 108.681 ns |     881 B |         - |
|       FindStructByRefLoop |   32 | 105.950 ns | 0.6382 ns | 0.9552 ns | 105.801 ns | 104.993 ns | 108.917 ns | 106.870 ns |     860 B |         - |
|   FindStructBySpanRefLoop |   32 | 100.764 ns | 0.8777 ns | 1.3137 ns | 100.368 ns |  98.910 ns | 104.139 ns | 102.609 ns |     766 B |         - |
|    FindStructBySpanRefAdd |   32 |  99.209 ns | 0.4823 ns | 0.7070 ns |  99.174 ns |  97.868 ns | 100.434 ns | 100.052 ns |     720 B |         - |
|   FindStructBySpanRefAdd2 |   32 | 105.386 ns | 0.2832 ns | 0.4151 ns | 105.363 ns | 104.830 ns | 106.254 ns | 105.894 ns |     708 B |         - |
|  FindStructBySpanRefWhile |   32 |  99.641 ns | 0.5611 ns | 0.8047 ns |  99.547 ns |  98.346 ns | 100.864 ns | 100.603 ns |     719 B |         - |
| FindStructBySpanRefWhile2 |   32 |  99.168 ns | 0.3701 ns | 0.5425 ns |  98.939 ns |  98.419 ns | 100.258 ns |  99.865 ns |     721 B |         - |
|           **FindClassByLoop** |   **64** | **199.429 ns** | **0.5133 ns** | **0.7361 ns** | **199.165 ns** | **198.339 ns** | **201.071 ns** | **200.389 ns** |     **858 B** |         **-** |
|          FindStructByLoop |   64 | 219.490 ns | 4.8143 ns | 7.0568 ns | 225.076 ns | 210.888 ns | 228.586 ns | 227.019 ns |     881 B |         - |
|       FindStructByRefLoop |   64 | 212.125 ns | 0.5640 ns | 0.8442 ns | 211.867 ns | 210.895 ns | 214.172 ns | 213.168 ns |     860 B |         - |
|   FindStructBySpanRefLoop |   64 | 199.713 ns | 0.5858 ns | 0.8768 ns | 199.642 ns | 198.417 ns | 201.401 ns | 200.891 ns |     766 B |         - |
|    FindStructBySpanRefAdd |   64 | 199.043 ns | 0.9012 ns | 1.3210 ns | 199.233 ns | 196.993 ns | 201.675 ns | 200.549 ns |     720 B |         - |
|   FindStructBySpanRefAdd2 |   64 | 212.146 ns | 0.8417 ns | 1.2598 ns | 212.168 ns | 210.360 ns | 214.462 ns | 214.050 ns |     708 B |         - |
|  FindStructBySpanRefWhile |   64 | 198.568 ns | 0.7525 ns | 1.1262 ns | 198.298 ns | 197.052 ns | 200.391 ns | 200.067 ns |     719 B |         - |
| FindStructBySpanRefWhile2 |   64 | 198.993 ns | 0.6444 ns | 0.9445 ns | 199.125 ns | 197.345 ns | 200.385 ns | 200.141 ns |     721 B |         - |
