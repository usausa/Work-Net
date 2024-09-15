# Benchmark

| Method    | Mean      | Error     | StdDev    | Min       | Max       | P90       | Code Size | Allocated |
|---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|----------:|
| ByPointer | 0.3593 ns | 0.0296 ns | 0.0317 ns | 0.3228 ns | 0.4083 ns | 0.4019 ns |      73 B |         - |
| ByUnsafe  | 0.2955 ns | 0.0227 ns | 0.0212 ns | 0.2685 ns | 0.3295 ns | 0.3257 ns |      56 B |         - |

# ASM

## .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
```assembly
; Benchmark.AsBenchmark.ByPointer()
;     {
;     ^
;         fixed (byte* p = &buffer[0])
;                ^^^^^^^^^^^^^^^^^^^^
;             var ptr = (WorkAsData*)p;
;             ^^^^^^^^^^^^^^^^^^^^^^^^^
;             ptr->Data1 = 1;
;             ^^^^^^^^^^^^^^^
;             ptr->Data2 = 2;
;             ^^^^^^^^^^^^^^^
;             ptr->Data3 = 3;
;             ^^^^^^^^^^^^^^^
;             ptr->Data4 = 4;
;             ^^^^^^^^^^^^^^^
;     }
;     ^
       7FF98A537520 sub       rsp,28
       7FF98A537524 mov       rax,[rcx+8]
       7FF98A537528 cmp       dword ptr [rax+8],0
       7FF98A53752C jbe       short M00_L00
       7FF98A53752E add       rax,10
       7FF98A537532 mov       [rsp+20],rax
       7FF98A537537 mov       rax,[rsp+20]
       7FF98A53753C mov       dword ptr [rax],1
       7FF98A537542 mov       dword ptr [rax+4],2
       7FF98A537549 mov       dword ptr [rax+8],3
       7FF98A537550 mov       dword ptr [rax+0C],4
       7FF98A537557 xor       eax,eax
       7FF98A537559 mov       [rsp+20],rax
       7FF98A53755E add       rsp,28
       7FF98A537562 ret
M00_L00:
       7FF98A537563 call      CORINFO_HELP_RNGCHKFAIL
       7FF98A537568 int       3
; Total bytes of code 73
```

## .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
```assembly
; Benchmark.AsBenchmark.ByUnsafe()
;         ref var data = ref Unsafe.As<byte, WorkAsData>(ref buffer[0]);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         data.Data1 = 1;
;         ^^^^^^^^^^^^^^^
;         data.Data2 = 2;
;         ^^^^^^^^^^^^^^^
;         data.Data3 = 3;
;         ^^^^^^^^^^^^^^^
;         data.Data4 = 4;
;         ^^^^^^^^^^^^^^^
       7FF98A5374E0 sub       rsp,28
       7FF98A5374E4 mov       rax,[rcx+8]
       7FF98A5374E8 cmp       dword ptr [rax+8],0
       7FF98A5374EC jbe       short M00_L00
       7FF98A5374EE add       rax,10
       7FF98A5374F2 mov       dword ptr [rax],1
       7FF98A5374F8 mov       dword ptr [rax+4],2
       7FF98A5374FF mov       dword ptr [rax+8],3
       7FF98A537506 mov       dword ptr [rax+0C],4
       7FF98A53750D add       rsp,28
       7FF98A537511 ret
M00_L00:
       7FF98A537512 call      CORINFO_HELP_RNGCHKFAIL
       7FF98A537517 int       3
; Total bytes of code 56
```

# Code

```csharp
/// <summary>
/// Casts the given object to the specified type, performs no dynamic type checking.
/// </summary>
[Intrinsic]
// CoreCLR:METHOD__UNSAFE__OBJECT_AS
// AOT:As
// Mono:As
[NonVersionable]
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[return: NotNullIfNotNull(nameof(o))]
public static T As<T>(object? o) where T : class?
{
    throw new PlatformNotSupportedException();

    // ldarg.0
    // ret
}

/// <summary>
/// Reinterprets the given reference as a reference to a value of type <typeparamref name="TTo"/>.
/// </summary>
[Intrinsic]
// CoreCLR:METHOD__UNSAFE__BYREF_AS
// AOT:As
// Mono:As
[NonVersionable]
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static ref TTo As<TFrom, TTo>(ref TFrom source)
{
    throw new PlatformNotSupportedException();

    // ldarg.0
    // ret
}
```
