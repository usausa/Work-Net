# Benchmark

| Method | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Code Size | Allocated |
|------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|
| ByCast | 1.3937 ns | 0.0748 ns | 0.1024 ns | 1.3513 ns | 1.2721 ns | 1.5587 ns | 1.5397 ns |     175 B |         - |
| ByAs   | 0.0105 ns | 0.0117 ns | 0.0125 ns | 0.0000 ns | 0.0000 ns | 0.0339 ns | 0.0253 ns |      43 B |         - |

# ASM

## .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
```assembly
; Benchmark.CastVsAsBenchmark.ByCast()
;     public object ByCast() => ((IFactory)o).Create();
;                               ^^^^^^^^^^^^^^^^^^^^^^
       7FF998547B10 sub       rsp,28
       7FF998547B14 mov       rdx,[rcx+8]
       7FF998547B18 mov       rcx,offset MT_Benchmark.CastVsAsBenchmark+IFactory
       7FF998547B22 call      qword ptr [7FF9985143C0]; System.Runtime.CompilerServices.CastHelpers.ChkCastInterface(Void*, System.Object)
       7FF998547B28 mov       rcx,rax
       7FF998547B2B mov       rax,offset MT_Benchmark.CastVsAsBenchmark+Factory
       7FF998547B35 cmp       [rcx],rax
       7FF998547B38 jne       short M00_L00
       7FF998547B3A xor       eax,eax
       7FF998547B3C add       rsp,28
       7FF998547B40 ret
M00_L00:
       7FF998547B41 mov       r11,7FF9983D04C0
       7FF998547B4B add       rsp,28
       7FF998547B4F jmp       qword ptr [r11]
; Total bytes of code 66
```

## .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
```assembly
; Benchmark.CastVsAsBenchmark.ByAs()
;     public void ByAs() => Unsafe.As<IFactory>(o).Create();
;                           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       7FF998547560 sub       rsp,28
       7FF998547564 mov       rcx,[rcx+8]
       7FF998547568 mov       r11,offset MT_Benchmark.CastVsAsBenchmark+Factory
       7FF998547572 cmp       [rcx],r11
       7FF998547575 jne       short M00_L01
M00_L00:
       7FF998547577 add       rsp,28
       7FF99854757B ret
M00_L01:
       7FF99854757C mov       r11,7FF9983D04C0
       7FF998547586 call      qword ptr [r11]
       7FF998547589 jmp       short M00_L00
; Total bytes of code 43
```
