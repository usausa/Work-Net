# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET library (`MacDotNet.SystemInfo`) for retrieving macOS system information via P/Invoke calls to native macOS libraries. Currently provides uptime retrieval; sandbox projects exist for future expansion (battery, CPU, GPU, memory, network, etc.). macOS-only.

## Build Commands

```bash
# Build main solution
dotnet build MacDotNet.slnx

# Build release
dotnet build MacDotNet.slnx -c Release

# Build sandbox projects
dotnet build Sandbox/Sandbox.slnx

# Run example app
dotnet run --project Example.SystemInfo.ConsoleApp -- uptime
```

No test projects exist currently.

## Architecture

- **MacDotNet.SystemInfo/** — NuGet library (targets net10.0 + net8.0)
  - `NativeMethods.cs` — P/Invoke declarations (`sysctlbyname` from libc, `timeval` struct)
  - `UptimeInfo.cs` — Retrieves boot time via `NativeMethods`, computes uptime as `TimeSpan`
  - `PlatformProvider.cs` — Public API facade (e.g., `GetUptime()`)
- **Example.SystemInfo.ConsoleApp/** — CLI demo using `Smart.CommandLine.Hosting` (net10.0)
- **Sandbox/** — Separate solution with work-in-progress projects for additional system info categories, all targeting net10.0 with `AllowUnsafeBlocks`

## Code Style & Analysis

- StyleCop.Analyzers enforced globally via `Directory.Build.props`
- `EnforceCodeStyleInBuild` is enabled — code style violations fail the build
- Rules configured in `Analyzers.ruleset`
- `.editorconfig` defines formatting: 4-space indentation, CRLF line endings, UTF-8
- `LangVersion: preview`, `Nullable: enable`, `ImplicitUsings: enable`
- Library is marked `CLSCompliant(false)`; example app uses `[SupportedOSPlatform("macos")]`

## Versioning & Packaging

- Global version (`0.0.2`) set in `Directory.Build.props`
- NuGet package ID: `MacDotNet.SystemInfo`
- SourceLink (GitHub) configured for Release builds
