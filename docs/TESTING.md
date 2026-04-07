# Testing Guide

## Automated Tests

The `vs-md-extension-buddy.Tests` project contains xUnit tests covering the core parsing logic.

### Running tests

```shell
# All tests
dotnet test vs-md-extension-buddy.Tests

# Verbose output
dotnet test vs-md-extension-buddy.Tests --verbosity normal

# Single test by name
dotnet test vs-md-extension-buddy.Tests --filter "FullyQualifiedName~ParseSections_FindsMonikerSection"

# Tests matching a pattern
dotnet test vs-md-extension-buddy.Tests --filter "FullyQualifiedName~TabSection"
```

### Test structure

| File | Covers |
|------|--------|
| `LearnSectionParserTests.cs` | Moniker, zone pivot, and tab parsing; code fence exclusion; front matter exclusion; nested sections; `FindSectionAtLine`; `FindSectionsByName`; `GetUniqueSections` |
| `MarkdownFoldingHelperTests.cs` | Code blocks, front matter, headings, tables, blockquotes, region markers, HTML blocks, lists, combined scenarios |

---

## Manual Testing Steps

### Prerequisites

1. Build the extension in Debug mode
2. Press F5 to launch the Visual Studio Experimental Instance
3. Open a markdown file (use `docs/sample.md` or create your own)

---

### 1. Outlining (Folding) Test

1. Open `docs/sample.md` in the Experimental Instance
2. Verify collapse indicators (▶/▼) appear in the margin next to:
   - `::: moniker range="..."`
   - `:::zone pivot="..."`
   - `# [Label](#tab/id)`
   - Standard headings (`#`, `##`, etc.)
   - Fenced code blocks
   - Tables
3. Click a collapse indicator to collapse the section
4. Verify the section collapses and shows the collapsed hint text
5. Click again to expand

**Expected**: Sections fold/unfold smoothly with correct boundaries.

---

### 2. Hover Preview Test

1. Collapse a Learn section (moniker, zone, or tab)
2. Hover over the first line of the collapsed section
3. Verify a QuickInfo tooltip appears showing:
   - Section type and name
   - Up to 20 lines of the section's content
   - A truncation indicator if the section has more than 20 lines

**Expected**: Tooltip displays correct preview content.

---

### 3. Keyboard Shortcut Test

| Shortcut | Test steps | Expected |
|----------|-----------|----------|
| `Ctrl+Alt+[` | Place cursor inside a section, press shortcut | Section toggles (collapses if expanded, expands if collapsed) |
| `Ctrl+Alt+]` | Place cursor in a collapsed section, press shortcut | Section expands |
| `Ctrl+Alt+F` | Press shortcut | Section picker dialog appears; selecting a section expands it and collapses others of the same type |

---

### 4. Context Menu Test

1. Open a markdown file
2. Right-click inside a Learn section
3. Verify the **Markdown Region Buddy** submenu appears
4. Test each menu item:
   - **Toggle Current Section**: Should toggle section at cursor
   - **Expand Named Section**: Should expand all sections with the same type and name
   - **Collapse Named Section**: Should collapse all sections with the same type and name
   - **Expand All Regions**: Should expand all Learn sections
   - **Collapse All Regions**: Should collapse all Learn sections

**Expected**: All context menu commands function correctly.

---

### 5. Named Section Test

1. Open a file with multiple zone pivot sections (e.g., `portal`, `csharp`, `python`)
2. Place cursor inside a `csharp` zone pivot
3. Use **Collapse Named Section** command
4. Verify all `csharp` zone pivots collapse, but `portal` and `python` remain unchanged
5. Use **Expand Named Section** command
6. Verify all `csharp` zone pivots expand

**Expected**: Operations affect only sections with matching type and name.

---

### 6. Focus Mode Test

1. Open a file with multiple zone pivots
2. Press `Ctrl+Alt+F`
3. Select `zone: python` from the picker
4. Verify:
   - All `python` zone pivots are expanded
   - Other zone pivots (`portal`, `csharp`) are collapsed
   - Monikers and tabs are unaffected

**Expected**: Only the selected section type/name is visible.

---

### 7. Background Decoration Test

#### Enable Decorations
1. Open **Tools → Options → Markdown Region Buddy**
2. Set **Enable Decorations** to `True`
3. Open a markdown file with Learn sections
4. Verify sections have subtle background colors:
   - Monikers: Blue tint
   - Zone Pivots: Green tint
   - Tabs: Pink tint

#### Change Opacity
1. In the options page, change **Decoration Opacity** to `0.15`
2. Verify backgrounds become more visible

#### Disable Decorations
1. Set **Enable Decorations** back to `False`
2. Verify backgrounds disappear

**Expected**: Decorations toggle correctly and respect settings.

---

### 8. Nested Section Test

1. Open a file with nested sections:
   ```markdown
   ::: moniker range="v2"

   :::zone pivot="advanced"
   Nested content
   ::: zone-end

   ::: moniker-end
   ```
2. Verify both levels have independent collapse indicators
3. Collapse the outer moniker section
4. Expand the outer section
5. Verify the inner zone remembers its collapsed/expanded state

**Expected**: Nested sections work independently.

---

### 9. Edge Cases

| Scenario | Expected behavior |
|----------|-------------------|
| Empty section (start and end markers with no content) | Should still show collapse indicator |
| Tab at end of file (no `---` terminator) | Should fold to end of document |
| Moniker/zone inside a fenced code block | Should NOT be detected as a section |
| `---` inside a fenced code block | Should NOT end a tab section |
| Front matter `---` delimiters | Should NOT be treated as tab terminators |

---

## Reporting Issues

When reporting issues, include:

1. Steps to reproduce
2. Expected vs. actual behavior
3. Sample markdown that demonstrates the issue
4. Visual Studio version and edition
5. Extension version
6. Screenshots if applicable
