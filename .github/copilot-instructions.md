# Copilot Instructions

## Project Overview

This is a Visual Studio 2022+ extension (VSIX) that ports the "Markdown Region Buddy" VS Code extension. It helps Microsoft Learn content authors manage markdown regions — monikers, zone pivots, and tabs — with folding (outlining), hover previews, commands, and optional background decorations.

Targets Visual Studio 2022 17.14+, .NET Framework 4.8, using VSSDK (classic MEF) extensibility model. The solution uses the `.slnx` format.

## Build

```shell
# Full MSBuild (requires VS 2022+ installed)
msbuild vs-md-extension-buddy.slnx /p:Configuration=Debug /restore

# Tests only (dotnet CLI)
dotnet test vs-md-extension-buddy.Tests --verbosity normal
```

Building produces a `.vsix` package in `bin\Debug\`.

## Debug

Press F5 in Visual Studio. The project launches the VS Experimental Instance (`devenv.exe /rootsuffix Exp`).

## Test

```shell
# All tests
dotnet test vs-md-extension-buddy.Tests

# Single test
dotnet test vs-md-extension-buddy.Tests --filter "FullyQualifiedName~ParseSections_FindsMonikerSection"
```

The test project links `Core\` source files directly (no project reference) since the main project targets .NET Framework 4.8 and tests target .NET 8.

## Architecture

- **`Core\`** — Pure C# parsing logic with zero VS SDK dependencies. Testable in isolation.
  - `LearnSection.cs` / `SectionType.cs` — Data model for parsed sections (moniker, zone pivot, tab).
  - `LearnSectionParser.cs` — Regex-based parser for Learn-specific markdown regions. Handles code fence and front matter exclusion.
  - `MarkdownFoldingHelper.cs` — Standard markdown fold detection (headings, code blocks, tables, front matter, blockquotes, HTML blocks, region markers, lists).
  - `FoldRange.cs` — Lightweight fold range struct returned by the helper.
- **`vs_md_extension_buddyPackage.cs`** — `AsyncPackage` entry point. Registers commands and services.
- **`source.extension.vsixmanifest`** — VSIX metadata and install targets.
- **`vs-md-extension-buddy.Tests\`** — xUnit test project targeting .NET 8.

## Conventions

- The package loads asynchronously (`AllowsBackgroundLoading = true`). Code touching the UI thread must call `await JoinableTaskFactory.SwitchToMainThreadAsync()`.
- Core parsing classes operate on `IReadOnlyList<string>` (lines of text), not VS SDK types, keeping them testable and portable.
- Use `PackageReference` packages (`Microsoft.VisualStudio.SDK`, `Microsoft.VSSDK.BuildTools`); don't add individual interop assembly references.
- Section types follow Microsoft Learn syntax: monikers (`::: moniker range="..."`), zone pivots (`:::zone pivot="..."`), tabs (`# [Label](#tab/id)`).

## Reference Extension

The companion VS Code extension is at https://github.com/alvinashcraft/markdown-region-buddy. Use its `src/` directory and `docs/ARCHITECTURE.md` as the authoritative reference for parsing behavior and feature design.
