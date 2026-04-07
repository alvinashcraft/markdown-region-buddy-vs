# Extension Architecture

## Component Overview

```
┌──────────────────────────────────────────────────────────────┐
│              vs_md_extension_buddyPackage.cs                  │
│                   (AsyncPackage Entry)                        │
│                                                              │
│  • Registers commands via OleMenuCommandService              │
│  • Provides LearnOptionPage (Tools → Options)                │
│  • MEF components auto-discovered by VS                      │
└──────────────────┬───────────────────────────────────────────┘
                   │
        ┌──────────┴──────────┬──────────────┬────────────────┐
        │                     │              │                │
        ▼                     ▼              ▼                ▼
┌───────────────┐   ┌────────────────┐   ┌────────────┐   ┌──────────────────┐
│ LearnOutlining│   │   LearnQuick   │   │   Learn    │   │  LearnFolding    │
│    Tagger     │   │   InfoSource   │   │ Adornment  │   │   Commands       │
│   Provider    │   │   Provider     │   │  Manager   │   │                  │
├───────────────┤   ├────────────────┤   ├────────────┤   ├──────────────────┤
│ ITagger<      │   │ IAsyncQuick    │   │ IWpfText   │   │ Uses             │
│ IOutlining    │   │ InfoSource     │   │ ViewCreation│  │ IOutlining       │
│ RegionTag>    │   │ Provider       │   │ Listener   │   │ Manager to       │
│               │   │                │   │            │   │ expand/collapse  │
│ [ContentType  │   │ Shows 20-line  │   │ WPF rect   │   │ sections         │
│  "markdown"]  │   │ preview on     │   │ adornments │   │                  │
│               │   │ hover          │   │ for colored│   │                  │
│ Debounced     │   │                │   │ backgrounds│   │                  │
│ re-parse on   │   │                │   │            │   │                  │
│ buffer change │   │                │   │            │   │                  │
└───────┬───────┘   └────────────────┘   └────────────┘   └──────────────────┘
        │
        ▼
┌──────────────────────────────────────────────────────────────┐
│                       Core\ (Pure C#)                        │
│              No Visual Studio SDK dependencies               │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌───────────────┐          ┌──────────────────┐             │
│  │ LearnSection  │          │   Markdown       │             │
│  │    Parser     │          │ FoldingHelper    │             │
│  ├───────────────┤          ├──────────────────┤             │
│  │ • Parse       │          │ • Headings       │             │
│  │   monikers    │          │ • Code blocks    │             │
│  │ • Parse       │          │ • Tables         │             │
│  │   zones       │          │ • Front matter   │             │
│  │ • Parse       │          │ • Blockquotes    │             │
│  │   tabs        │          │ • HTML blocks    │             │
│  │ • Find by     │          │ • Region markers │             │
│  │   line/name   │          │ • Lists          │             │
│  └───────────────┘          └──────────────────┘             │
│                                                              │
│  ┌───────────────┐          ┌──────────────────┐             │
│  │ LearnSection  │          │   FoldRange      │             │
│  │ (data model)  │          │   (struct)       │             │
│  └───────────────┘          └──────────────────┘             │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## Data Flow

### 1. Section Parsing

```
ITextBuffer.CurrentSnapshot (lines of text)
      │
      ▼
LearnSectionParser.ParseSections(lines)
      │
      ├─► Regex match ::: moniker range="..." → Moniker section
      ├─► Regex match :::zone pivot="..." → Zone pivot section
      └─► Regex match # [Label](#tab/id) → Tab section
      │
      ▼
List<LearnSection>
```

### 2. Outlining (Folding)

```
VS requests outlining tags via ITagger<IOutliningRegionTag>
      │
      ▼
LearnOutliningTagger.GetTags()
      │
      ├─► Calls LearnSectionParser.ParseSections()  → region outlines
      ├─► Calls MarkdownFoldingHelper.GetFoldingRanges() → standard outlines
      │
      ▼
Merges both into ITagSpan<IOutliningRegionTag> collection
      │
      ▼
VS displays collapse indicators in the margin
```

### 3. QuickInfo (Hover)

```
User hovers over collapsed section's start line
      │
      ▼
LearnQuickInfoSource.GetQuickInfoItemAsync()
      │
      ├─► Finds section at trigger point
      ├─► Checks if section is collapsed via IOutliningManager
      ├─► Extracts first 20 lines
      ├─► Formats as monospaced text
      │
      ▼
VS displays QuickInfo tooltip
```

### 4. Commands

```
User triggers command (keyboard / context menu)
      │
      ▼
LearnFoldingCommands handler
      │
      ├─► Parse document to find sections
      ├─► Find relevant section(s)
      ├─► Use IOutliningManager.TryCollapse() / Expand()
      │
      ▼
Sections expand/collapse
```

### 5. Background Decorations

```
Document opened / text changed / layout changed
      │
      ▼
LearnAdornmentManager
      │
      ├─► Check if decorations enabled (Tools → Options)
      ├─► Parse sections
      ├─► Group by type (moniker / zone / tab)
      ├─► Draw WPF Rectangles on IAdornmentLayer
      │
      ▼
Background colors displayed (if enabled)
```

## Section Type Definitions

```csharp
public enum SectionType
{
    Moniker,
    Zone,
    Tab
}

public class LearnSection
{
    public SectionType Type { get; }
    public string Name { get; }        // e.g., "foundry", "python", "windows"
    public int StartLine { get; }      // 0-based line number
    public int EndLine { get; }        // 0-based line number
    public int IndentLevel { get; }    // For nested sections
}
```

## Regex Patterns

### Moniker

```csharp
Start: /^(\s*)::: moniker range="([^"]+)"/
End:   /^(\s*)::: moniker-end/

Example:
::: moniker range="foundry-classic"
                    ^^^^^^^^^^^^^^^
                    Captured as Name
```

### Zone Pivot

```csharp
Start: /^(\s*):::zone pivot="([^"]+)"/
End:   /^(\s*)::: zone-end/

Example:
:::zone pivot="python"
               ^^^^^^
               Captured as Name
```

### Tab

```csharp
Header: /^(\s*)#{1,6} \[([^\]]+)\]\(#tab\/([^)]+)\)/
End:    /^(\s*)---\s*$/ or next tab header

Example:
# [Linux](#tab/linux)
             ^^^^^
             Captured as Name (id)
```

## VS Code → Visual Studio Mapping

| VS Code Concept | Visual Studio Equivalent |
|---|---|
| `FoldingRangeProvider` | `ITagger<IOutliningRegionTag>` (MEF) |
| `HoverProvider` | `IAsyncQuickInfoSourceProvider` / `IAsyncQuickInfoSource` |
| `vscode.commands.registerCommand` | `OleMenuCommandService` + `.vsct` command table |
| Text decorations (CSS rgba) | `IWpfTextViewCreationListener` + `IAdornmentLayer` (WPF) |
| `workspace.getConfiguration` | `DialogPage` (Tools → Options) |
| `showQuickPick` | Custom dialog or `IVsUIShell` |
| Content type activation | `[ContentType("markdown")]` MEF attribute |

## Key Design Decisions

1. **Core parsing is SDK-free.** `LearnSectionParser` and `MarkdownFoldingHelper` operate on `IReadOnlyList<string>` with zero Visual Studio dependencies. This makes them unit-testable with plain xUnit (no VS test host needed) and keeps them as a clean 1:1 port from the TypeScript originals.

2. **No folding provider conflicts.** Unlike VS Code, Visual Studio's outlining model allows multiple `ITagger<IOutliningRegionTag>` implementations to coexist on the same content type. There's no need for a "Set as Default Folding Provider" command.

3. **WPF adornments for decorations.** Background colors use WPF `Rectangle` elements on an `IAdornmentLayer` rather than CSS-style rgba values. The opacity setting maps directly to WPF's `Opacity` property.

4. **Debounced re-parse.** The outlining tagger debounces `ITextBuffer.Changed` events to avoid re-parsing on every keystroke, matching the 300ms debounce in the VS Code version.
