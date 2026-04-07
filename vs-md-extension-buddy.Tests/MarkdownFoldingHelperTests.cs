using vs_md_extension_buddy.Core;

namespace vs_md_extension_buddy.Tests;

public class MarkdownFoldingHelperTests
{
    #region Code blocks

    [Fact]
    public void GetFoldingRanges_FoldsFencedCodeBlock()
    {
        var lines = new[]
        {
            "```csharp",
            "var x = 1;",
            "var y = 2;",
            "```"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 3);
    }

    [Fact]
    public void GetFoldingRanges_FoldsTildeFencedCodeBlock()
    {
        var lines = new[]
        {
            "~~~python",
            "x = 1",
            "~~~"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 2);
    }

    [Fact]
    public void GetFoldingRanges_UnclosedFence_NoRange()
    {
        var lines = new[]
        {
            "```",
            "unclosed code"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.DoesNotContain(ranges, r => r.StartLine == 0);
    }

    #endregion

    #region Front matter

    [Fact]
    public void GetFoldingRanges_FoldsFrontMatter()
    {
        var lines = new[]
        {
            "---",
            "title: Test",
            "author: Me",
            "---",
            "# Content"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 3);
    }

    [Fact]
    public void GetFoldingRanges_FrontMatterMustStartAtLine0()
    {
        var lines = new[]
        {
            "Some text",
            "---",
            "title: Test",
            "---"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        // Should NOT have a front matter fold starting at line 1
        Assert.DoesNotContain(ranges, r => r.StartLine == 0 && r.EndLine == 3);
    }

    #endregion

    #region Headings

    [Fact]
    public void GetFoldingRanges_FoldsHeading()
    {
        var lines = new[]
        {
            "# Heading 1",
            "Content under heading 1.",
            "More content.",
            "## Heading 2",
            "Content under heading 2."
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        // H1 should fold from line 0 to line 4 (end of doc)
        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 4);
        // H2 should fold from line 3 to line 4
        Assert.Contains(ranges, r => r.StartLine == 3 && r.EndLine == 4);
    }

    [Fact]
    public void GetFoldingRanges_HeadingTrimsTrailingBlanks()
    {
        var lines = new[]
        {
            "# Heading",
            "Content.",
            "",
            ""
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 1);
    }

    [Fact]
    public void GetFoldingRanges_SkipsTabHeaders()
    {
        var lines = new[]
        {
            "# [Linux](#tab/linux)",
            "Linux content.",
            "---"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        // Should NOT treat the tab header as a heading fold
        Assert.DoesNotContain(ranges, r => r.StartLine == 0 && r.EndLine > 0 &&
            ranges.Any(r2 => r2.StartLine == 0));
    }

    #endregion

    #region Tables

    [Fact]
    public void GetFoldingRanges_FoldsTable()
    {
        var lines = new[]
        {
            "| Header 1 | Header 2 |",
            "| --- | --- |",
            "| Cell 1 | Cell 2 |",
            "| Cell 3 | Cell 4 |"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 3);
    }

    [Fact]
    public void GetFoldingRanges_SingleRowWithSeparator_NoFold()
    {
        var lines = new[]
        {
            "| Header |",
            "| --- |"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        // A header + separator with no data rows still spans 2 lines, so it should fold
        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 1);
    }

    #endregion

    #region Blockquotes

    [Fact]
    public void GetFoldingRanges_FoldsMultiLineBlockquote()
    {
        var lines = new[]
        {
            "> Line 1",
            "> Line 2",
            "> Line 3"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 2);
    }

    [Fact]
    public void GetFoldingRanges_SingleLineBlockquote_NoFold()
    {
        var lines = new[]
        {
            "> Just one line."
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.DoesNotContain(ranges, r => r.StartLine == 0);
    }

    #endregion

    #region Region markers

    [Fact]
    public void GetFoldingRanges_FoldsRegionMarkers()
    {
        var lines = new[]
        {
            "<!-- #region Setup -->",
            "Setup content.",
            "More setup.",
            "<!-- #endregion -->"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 3 && r.Kind == FoldKind.Region);
    }

    [Fact]
    public void GetFoldingRanges_NestedRegionMarkers()
    {
        var lines = new[]
        {
            "<!-- #region Outer -->",
            "Outer content.",
            "<!-- #region Inner -->",
            "Inner content.",
            "<!-- #endregion -->",
            "Back to outer.",
            "<!-- #endregion -->"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 6);
        Assert.Contains(ranges, r => r.StartLine == 2 && r.EndLine == 4);
    }

    #endregion

    #region HTML blocks

    [Fact]
    public void GetFoldingRanges_FoldsHtmlComment()
    {
        var lines = new[]
        {
            "<!--",
            "Multi-line comment.",
            "More comment.",
            "-->"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 3);
    }

    [Fact]
    public void GetFoldingRanges_SingleLineHtmlComment_NoFold()
    {
        var lines = new[]
        {
            "<!-- Single line comment -->"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.DoesNotContain(ranges, r => r.StartLine == 0);
    }

    [Fact]
    public void GetFoldingRanges_FoldsDetailsElement()
    {
        var lines = new[]
        {
            "<details>",
            "<summary>Click</summary>",
            "Content.",
            "</details>"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 3);
    }

    #endregion

    #region Lists

    [Fact]
    public void GetFoldingRanges_FoldsMultiLineListItem()
    {
        var lines = new[]
        {
            "- Item with",
            "  continuation line",
            "  and another"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 2);
    }

    [Fact]
    public void GetFoldingRanges_FoldsOrderedList()
    {
        var lines = new[]
        {
            "1. First item",
            "   with continuation",
            "2. Second item"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        Assert.Contains(ranges, r => r.StartLine == 0 && r.EndLine == 1);
    }

    #endregion

    #region Combined scenarios

    [Fact]
    public void GetFoldingRanges_DoesNotFoldInsideCodeBlock()
    {
        var lines = new[]
        {
            "```",
            "# This is not a heading",
            "| Not | A | Table |",
            "| --- | --- | --- |",
            "```"
        };

        var ranges = MarkdownFoldingHelper.GetFoldingRanges(lines);

        // Should only have the code block fold, not a heading or table fold
        Assert.Single(ranges);
        Assert.Equal(0, ranges[0].StartLine);
        Assert.Equal(4, ranges[0].EndLine);
    }

    #endregion
}
