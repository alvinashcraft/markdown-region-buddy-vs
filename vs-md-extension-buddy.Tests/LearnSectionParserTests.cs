using vs_md_extension_buddy.Core;

namespace vs_md_extension_buddy.Tests;

public class LearnSectionParserTests
{
    #region Moniker parsing

    [Fact]
    public void ParseSections_FindsMonikerSection()
    {
        var lines = new[]
        {
            "::: moniker range=\"foundry-classic\"",
            "Some content here.",
            "::: moniker-end"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Single(sections);
        Assert.Equal(SectionType.Moniker, sections[0].Type);
        Assert.Equal("foundry-classic", sections[0].Name);
        Assert.Equal(0, sections[0].StartLine);
        Assert.Equal(2, sections[0].EndLine);
    }

    [Fact]
    public void ParseSections_FindsMultipleMonikerSections()
    {
        var lines = new[]
        {
            "::: moniker range=\"foundry-classic\"",
            "Classic content.",
            "::: moniker-end",
            "",
            "::: moniker range=\"foundry\"",
            "New content.",
            "::: moniker-end"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Equal(2, sections.Count);
        Assert.Equal("foundry-classic", sections[0].Name);
        Assert.Equal("foundry", sections[1].Name);
    }

    [Fact]
    public void ParseSections_MonikerWithoutEnd_ReturnsNothing()
    {
        var lines = new[]
        {
            "::: moniker range=\"foundry\"",
            "Content with no end marker."
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Empty(sections);
    }

    #endregion

    #region Zone pivot parsing

    [Fact]
    public void ParseSections_FindsZonePivotSection()
    {
        var lines = new[]
        {
            ":::zone pivot=\"csharp\"",
            "C# content.",
            "::: zone-end"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Single(sections);
        Assert.Equal(SectionType.Zone, sections[0].Type);
        Assert.Equal("csharp", sections[0].Name);
        Assert.Equal(0, sections[0].StartLine);
        Assert.Equal(2, sections[0].EndLine);
    }

    [Fact]
    public void ParseSections_FindsMultipleZones()
    {
        var lines = new[]
        {
            ":::zone pivot=\"portal\"",
            "Portal content.",
            "::: zone-end",
            ":::zone pivot=\"csharp\"",
            "C# content.",
            "::: zone-end",
            ":::zone pivot=\"python\"",
            "Python content.",
            "::: zone-end"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Equal(3, sections.Count);
        Assert.Equal("portal", sections[0].Name);
        Assert.Equal("csharp", sections[1].Name);
        Assert.Equal("python", sections[2].Name);
    }

    #endregion

    #region Tab parsing

    [Fact]
    public void ParseSections_FindsTabSection_EndedByHorizontalRule()
    {
        var lines = new[]
        {
            "# [Linux](#tab/linux)",
            "Linux content.",
            "---"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Single(sections);
        Assert.Equal(SectionType.Tab, sections[0].Type);
        Assert.Equal("linux", sections[0].Name);
        Assert.Equal(0, sections[0].StartLine);
        Assert.Equal(2, sections[0].EndLine);
    }

    [Fact]
    public void ParseSections_FindsTabSection_EndedByNextTab()
    {
        var lines = new[]
        {
            "# [Linux](#tab/linux)",
            "Linux content.",
            "# [Windows](#tab/windows)",
            "Windows content.",
            "---"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Equal(2, sections.Count);
        Assert.Equal("linux", sections[0].Name);
        Assert.Equal(0, sections[0].StartLine);
        Assert.Equal(1, sections[0].EndLine); // Ends one line before next tab
        Assert.Equal("windows", sections[1].Name);
        Assert.Equal(2, sections[1].StartLine);
        Assert.Equal(4, sections[1].EndLine);
    }

    [Fact]
    public void ParseSections_TabAtEndOfDocument()
    {
        var lines = new[]
        {
            "# [Linux](#tab/linux)",
            "Linux content.",
            "More content."
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Single(sections);
        Assert.Equal(0, sections[0].StartLine);
        Assert.Equal(2, sections[0].EndLine);
    }

    [Fact]
    public void ParseSections_TabWithDependentSyntax()
    {
        var lines = new[]
        {
            "# [Label](#tab/tab-id/tab-condition)",
            "Content.",
            "---"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Single(sections);
        Assert.Equal("tab-id/tab-condition", sections[0].Name);
    }

    #endregion

    #region Code fence exclusion

    [Fact]
    public void ParseSections_SkipsMonikerInsideCodeFence()
    {
        var lines = new[]
        {
            "```markdown",
            "::: moniker range=\"fake\"",
            "::: moniker-end",
            "```"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Empty(sections);
    }

    [Fact]
    public void ParseSections_SkipsTabEndInsideCodeFence()
    {
        var lines = new[]
        {
            "# [Linux](#tab/linux)",
            "```bash",
            "---",
            "```",
            "More content.",
            "---"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Single(sections);
        Assert.Equal(0, sections[0].StartLine);
        Assert.Equal(5, sections[0].EndLine);
    }

    #endregion

    #region Front matter exclusion

    [Fact]
    public void ParseSections_SkipsFrontMatter()
    {
        var lines = new[]
        {
            "---",
            "title: My Document",
            "---",
            "# [Tab](#tab/my-tab)",
            "Content.",
            "---"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Single(sections);
        Assert.Equal(SectionType.Tab, sections[0].Type);
        Assert.Equal(3, sections[0].StartLine);
        Assert.Equal(5, sections[0].EndLine);
    }

    #endregion

    #region Nested sections

    [Fact]
    public void ParseSections_FindsNestedSections()
    {
        var lines = new[]
        {
            "::: moniker range=\"version-2\"",
            ":::zone pivot=\"advanced\"",
            "Advanced content.",
            "::: zone-end",
            ":::zone pivot=\"basic\"",
            "Basic content.",
            "::: zone-end",
            "::: moniker-end"
        };

        var sections = LearnSectionParser.ParseSections(lines);

        Assert.Equal(3, sections.Count);
        Assert.Equal(SectionType.Moniker, sections[0].Type);
        Assert.Equal(SectionType.Zone, sections[1].Type);
        Assert.Equal(SectionType.Zone, sections[2].Type);
    }

    #endregion

    #region FindSectionAtLine

    [Fact]
    public void FindSectionAtLine_ReturnsInnermostSection()
    {
        var lines = new[]
        {
            "::: moniker range=\"version-2\"",
            ":::zone pivot=\"advanced\"",
            "Content at line 2.",
            "::: zone-end",
            "::: moniker-end"
        };

        var section = LearnSectionParser.FindSectionAtLine(lines, 2);

        Assert.NotNull(section);
        Assert.Equal(SectionType.Zone, section.Type);
        Assert.Equal("advanced", section.Name);
    }

    [Fact]
    public void FindSectionAtLine_ReturnsNull_WhenNotInSection()
    {
        var lines = new[]
        {
            "Normal content.",
            "::: moniker range=\"foundry\"",
            "Inside.",
            "::: moniker-end",
            "Normal again."
        };

        Assert.Null(LearnSectionParser.FindSectionAtLine(lines, 0));
        Assert.Null(LearnSectionParser.FindSectionAtLine(lines, 4));
    }

    #endregion

    #region FindSectionsByName

    [Fact]
    public void FindSectionsByName_ReturnsMatchingSections()
    {
        var lines = new[]
        {
            ":::zone pivot=\"csharp\"",
            "First C# block.",
            "::: zone-end",
            ":::zone pivot=\"python\"",
            "Python block.",
            "::: zone-end",
            ":::zone pivot=\"csharp\"",
            "Second C# block.",
            "::: zone-end"
        };

        var results = LearnSectionParser.FindSectionsByName(lines, SectionType.Zone, "csharp");

        Assert.Equal(2, results.Count);
        Assert.All(results, s => Assert.Equal("csharp", s.Name));
    }

    #endregion

    #region GetUniqueSections

    [Fact]
    public void GetUniqueSections_ReturnsDistinctTypeNamePairs()
    {
        var lines = new[]
        {
            ":::zone pivot=\"csharp\"",
            "Block 1.",
            "::: zone-end",
            ":::zone pivot=\"csharp\"",
            "Block 2.",
            "::: zone-end",
            ":::zone pivot=\"python\"",
            "Block 3.",
            "::: zone-end"
        };

        var unique = LearnSectionParser.GetUniqueSections(lines);

        Assert.Equal(2, unique.Count);
        Assert.Contains(unique, u => u.Name == "csharp");
        Assert.Contains(unique, u => u.Name == "python");
    }

    #endregion

    #region Full sample document

    [Fact]
    public void ParseSections_FullSampleDocument()
    {
        // Mirrors the structure of docs/sample.md from the VS Code extension
        var lines = new[]
        {
            "# Sample Markdown with Regions",                // 0
            "",                                               // 1
            "## Monikers Example",                            // 2
            "",                                               // 3
            "::: moniker range=\"foundry-classic\"",          // 4
            "",                                               // 5
            "Classic content.",                                // 6
            "",                                               // 7
            "::: moniker-end",                                // 8
            "",                                               // 9
            "::: moniker range=\"foundry\"",                  // 10
            "",                                               // 11
            "New content.",                                    // 12
            "",                                               // 13
            "::: moniker-end",                                // 14
            "",                                               // 15
            "## Zone Pivots Example",                         // 16
            "",                                               // 17
            ":::zone pivot=\"portal\"",                       // 18
            "Portal content.",                                // 19
            "::: zone-end",                                   // 20
            "",                                               // 21
            ":::zone pivot=\"csharp\"",                       // 22
            "C# content.",                                    // 23
            "::: zone-end",                                   // 24
            "",                                               // 25
            "## Tabs Example",                                // 26
            "",                                               // 27
            "# [Linux](#tab/linux)",                          // 28
            "Linux content.",                                  // 29
            "# [Windows](#tab/windows)",                      // 30
            "Windows content.",                                // 31
            "---"                                              // 32
        };

        var sections = LearnSectionParser.ParseSections(lines);

        // 2 monikers + 2 zones + 2 tabs = 6
        Assert.Equal(6, sections.Count);

        var monikers = sections.Where(s => s.Type == SectionType.Moniker).ToList();
        Assert.Equal(2, monikers.Count);

        var zones = sections.Where(s => s.Type == SectionType.Zone).ToList();
        Assert.Equal(2, zones.Count);

        var tabs = sections.Where(s => s.Type == SectionType.Tab).ToList();
        Assert.Equal(2, tabs.Count);
        Assert.Equal("linux", tabs[0].Name);
        Assert.Equal("windows", tabs[1].Name);
    }

    #endregion
}
