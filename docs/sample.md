# Sample Markdown with Regions

This is a sample file demonstrating the Markdown Region Buddy extension features.

## Monikers Example

::: moniker range="foundry-classic"

This content is for **Foundry Classic** users only.

You can write multiple paragraphs here.

- Lists work too
- Another item

::: moniker-end

::: moniker range="foundry"

This content is for the **new Foundry** platform.

Updated instructions go here with new features.

::: moniker-end

## Zone Pivots Example

Some shared content outside the zone pivots.

:::zone pivot="portal"

### Using the Azure Portal

1. Navigate to your resource
2. Click on the settings tab
3. Configure your options

This is the portal-specific content.

::: zone-end

:::zone pivot="csharp"

### Using C#

```csharp
var client = new ServiceClient();
await client.ConnectAsync();
```

Steps for C# developers go here.

::: zone-end

:::zone pivot="python"

### Using Python

```python
client = ServiceClient()
await client.connect()
```

Steps for Python developers go here.

::: zone-end

More shared content here.

## Tabs Example

Select your operating system:

# [Linux](#tab/linux)

Install on Linux:

```bash
sudo apt-get install package-name
```

Additional Linux-specific instructions.

# [Windows](#tab/windows)

Install on Windows:

```powershell
choco install package-name
```

Additional Windows-specific instructions.

# [macOS](#tab/macos)

Install on macOS:

```bash
brew install package-name
```

Additional macOS-specific instructions.

---

## Nested Example

::: moniker range="version-2"

### Version 2 Content

:::zone pivot="advanced"

This is advanced content for version 2.

::: zone-end

:::zone pivot="basic"

This is basic content for version 2.

::: zone-end

::: moniker-end

## Blockquotes Example

> This is a single-line blockquote (should not fold).

> This is a multi-line blockquote.
> It spans multiple lines and should be foldable.
> You can include **bold** and *italic* text.
> And even links like [example](https://example.com).

> This blockquote has a nested quote:
>
> > This is the nested part.
> > It continues here.
>
> Back to the outer level.

## HTML Blocks Example

<details>
<summary>Click to expand</summary>

This content is inside an HTML details element.
It should be foldable.

</details>

<div class="custom-container">
  <p>This is a multi-line HTML block.</p>
  <p>It should fold from the opening to the closing tag.</p>
</div>

<!--
This is a multi-line HTML comment.
It should be foldable.
Comments like this are useful for notes to authors.
-->

## Region Markers Example

<!-- #region Setup Instructions -->
These instructions explain how to set up the project.

You can fold this section using the region marker.
<!-- #endregion -->

<!-- #region Outer Region -->
This is the outer region.

<!-- #region Inner Region -->
This is a nested region inside the outer one.
Both should fold independently.
<!-- #endregion -->

Back to the outer region.
<!-- #endregion -->

## Lists Example

- Simple item 1
- Simple item 2 with
  continuation on the next line
  and another line here
- Simple item 3

1. First ordered item
   with a continuation line
   and another
2. Second ordered item
3. Third ordered item

- Parent item
  - Nested child 1
    - Deeply nested item
    - Another deeply nested
  - Nested child 2
- Another parent item

* Item with multiple paragraphs

  This is a continuation paragraph under the list item.
  It should be included in the fold.

* Next item

## End of Document

This extension allows you to:
- Collapse/expand sections using the fold indicators
- Hover over collapsed sections to see preview
- Right-click for context menu options
- Use keyboard shortcuts for quick access
- Focus on specific section types
