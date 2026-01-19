# Contributing to ParseZero

Thanks for your interest in contributing! This guide covers how to build, test, and propose changes.

## Prerequisites
- .NET SDK 8.0 (includes .NET Standard targeting packs)
- Windows or Linux with AVX2-capable CPU for SIMD benchmarks (optional)
- Git

## Getting Started
1. Fork and clone the repo.
2. Create a feature branch: `git checkout -b feature/your-change`.
3. Restore and build: `dotnet build ParseZero.sln`.
4. Run tests (net8.0 and net472):
   - `dotnet test tests/ParseZero.Tests/ParseZero.Tests.csproj -c Release`
5. Run samples (optional): `dotnet run --project samples/ParseZero.Samples/ParseZero.Samples.csproj`.
6. Run benchmarks (optional, slower):
   - `dotnet run --project benchmarks/ParseZero.Benchmarks/ParseZero.Benchmarks.csproj -c Release -- --filter "*ParseZero*"`

## Coding Guidelines
- Favor zero-allocation code paths; avoid LINQ on `ref struct` types.
- Keep `Span<T>`/`Memory<T>` usage safe—no escaped references.
- Maintain cross-target compatibility (`netstandard2.0`, `net8.0`).
- Add XML docs for public APIs (already enforced via build).
- Include tests for new functionality and edge cases.

## Pull Requests
- Keep PRs focused and small where possible.
- Update docs and samples when behavior changes.
- Ensure `dotnet format` (or equivalent) passes if formatting is affected.
- All tests must pass; include benchmark impact if performance-related.
- Clearly describe motivation and testing performed in the PR description.

## Reporting Issues
- Include .NET version, OS, reproduction steps, and sample CSV data if relevant.
- For performance issues, share file size, column count, and observed timings.

## Security
- Please report security issues privately (see SECURITY.md).

Thanks for helping make ParseZero better!
