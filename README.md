# Markdown Region Buddy for Visual Studio

A Visual Studio 2022 extension that helps authors manage markdown regions — **monikers**, **zone pivots**, and **tabs** — with outlining (folding), hover previews, focus commands, and optional background decorations. Built for [Microsoft Learn](https://learn.microsoft.com/) content authors and anyone working with regional markdown syntax.

This is the Visual Studio port of the [Markdown Region Buddy](https://marketplace.visualstudio.com/items?itemName=alvinashcraft.markdown-region-buddy) VS Code extension.

## Features

### Region outlining

Collapse and expand moniker, zone pivot, and tab sections directly in the editor margin. The extension also provides standard markdown outlining for headings, fenced code blocks, tables, front matter, blockquotes, HTML blocks, `<!-- #region -->` markers, and lists.

### Hover previews

Hover over a collapsed section's start line to see a QuickInfo preview of up to 20 lines of content without expanding it.

### Focus mode

Use the **Focus on Section** command to expand sections of a specific type/name while collapsing everything else — great for isolating the content you're working on.

### Context menu

All operations are available from a **Markdown Region Buddy** submenu in the right-click context menu when editing markdown files.

### Background decorations (optional)

Enable subtle, theme-aware background colors to visually distinguish moniker (blue), zone pivot (green), and tab (pink) regions. Configure under **Tools → Options → Markdown Region Buddy**.

## Supported section types

| Type | Start syntax | End syntax |
|------|-------------|------------|
| Moniker | `::: moniker range="..."` | `::: moniker-end` |
| Zone Pivot | `:::zone pivot="..."` | `::: zone-end` |
| Tab | `# [Label](#tab/id)` | `---` or next tab header |

> Tab headers support any heading level (`#` through `######`) and optional dependent-tab syntax: `# [Label](#tab/tab-id/tab-condition)`.

## Getting started

1. Install the extension by double-clicking the `.vsix` file or via the Visual Studio Marketplace.
2. Open any `.md` file — the extension activates automatically.
3. Look for outlining indicators in the margin next to region start lines.

## Keyboard shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Alt+[` | Toggle current section |
| `Ctrl+Alt+]` | Expand current section |
| `Ctrl+Alt+F` | Focus on section (opens picker) |

## Commands

| Command | Description |
|---------|-------------|
| Toggle Current Section | Collapse or expand the section at the cursor |
| Expand Current Section | Expand the section at the cursor |
| Collapse Current Section | Collapse the section at the cursor |
| Expand All Regions | Expand all moniker, zone pivot, and tab sections |
| Collapse All Regions | Collapse all moniker, zone pivot, and tab sections |
| Expand Named Section | Expand all sections matching the current section's name |
| Collapse Named Section | Collapse all sections matching the current section's name |
| Focus on Section | Pick a section type/name to focus; others collapse |
| Toggle Section Background Colors | Enable or disable background decorations |

## Configuration

Settings are available under **Tools → Options → Markdown Region Buddy**.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| Enable Decorations | boolean | `false` | Enable background colors for different section types |
| Decoration Opacity | number | `0.05` | Opacity for section background colors (0.01–0.3) |

## Requirements

- Visual Studio 2022 version 17.14 or later

## Building from source

```shell
# Restore and build
msbuild vs-md-extension-buddy.slnx /p:Configuration=Debug /restore

# Run tests
dotnet test vs-md-extension-buddy.Tests
```

Press F5 to launch the Visual Studio Experimental Instance for debugging.

## Documentation

- [Architecture](docs/ARCHITECTURE.md) — Technical design and component overview
- [Testing Guide](docs/TESTING.md) — Manual and automated testing instructions
- [Sample File](docs/sample.md) — Example markdown with all supported section types

## License

[MIT](LICENSE)
