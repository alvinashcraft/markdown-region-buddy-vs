using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace vs_md_extension_buddy.Core
{
    /// <summary>
    /// Provides standard markdown folding ranges (headings, code blocks, tables,
    /// front matter, blockquotes, HTML blocks, region markers, and lists).
    /// Pure C# with no Visual Studio SDK dependencies.
    /// </summary>
    public static class MarkdownFoldingHelper
    {
        private static readonly Regex Heading = new Regex(@"^(#{1,6})\s", RegexOptions.Compiled);
        private static readonly Regex FenceOpen = new Regex(@"^(\s*)(```|~~~)", RegexOptions.Compiled);
        private static readonly Regex TableRow = new Regex(@"^\s*\|", RegexOptions.Compiled);
        private static readonly Regex TableSeparator = new Regex(@"^\s*\|[\s\-:|]+\|\s*$", RegexOptions.Compiled);
        private static readonly Regex TabHeaderPattern = new Regex(@"^(\s*)#{1,6} \[([^\]]+)\]\(#tab\/([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex FrontMatterDelimiter = new Regex(@"^---\s*$", RegexOptions.Compiled);
        private static readonly Regex BlockquoteLine = new Regex(@"^\s*>", RegexOptions.Compiled);
        private static readonly Regex RegionStart = new Regex(@"^\s*<!--\s*#region\b", RegexOptions.Compiled);
        private static readonly Regex RegionEnd = new Regex(@"^\s*<!--\s*#endregion\b", RegexOptions.Compiled);
        private static readonly Regex ListItem = new Regex(@"^(\s*)([-*+]|\d+[.)]) ", RegexOptions.Compiled);
        private static readonly Regex HtmlBlockOpen = new Regex(
            @"^(\s*)<(address|article|aside|base|basefont|blockquote|body|caption|center|col|colgroup|dd|details|dialog|dir|div|dl|dt|fieldset|figcaption|figure|footer|form|frame|frameset|h[1-6]|head|header|hgroup|hr|html|iframe|legend|li|link|main|menu|menuitem|meta|nav|noframes|ol|optgroup|option|p|param|pre|script|search|section|source|style|summary|table|tbody|td|template|tfoot|th|thead|title|tr|track|ul)\b[^>]*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HtmlCommentOpen = new Regex(@"^(\s*)<!--", RegexOptions.Compiled);

        /// <summary>
        /// Parse standard markdown folding ranges from lines of text.
        /// </summary>
        public static List<FoldRange> GetFoldingRanges(IReadOnlyList<string> lines)
        {
            var ranges = new List<FoldRange>();

            // 1. Code blocks
            var codeBlockRanges = ParseCodeBlocks(lines);
            var excludedLines = BuildLineSet(codeBlockRanges);
            foreach (var cb in codeBlockRanges)
            {
                if (cb.EndLine > cb.StartLine)
                    ranges.Add(new FoldRange(cb.StartLine, cb.EndLine));
            }

            // 2. Front matter
            var frontMatter = ParseFrontMatter(lines);
            if (frontMatter.HasValue)
            {
                AddToLineSet(excludedLines, frontMatter.Value.StartLine, frontMatter.Value.EndLine);
                if (frontMatter.Value.EndLine > frontMatter.Value.StartLine)
                    ranges.Add(new FoldRange(frontMatter.Value.StartLine, frontMatter.Value.EndLine));
            }

            // 3. HTML blocks
            var htmlBlockRanges = ParseHtmlBlocks(lines, excludedLines);
            foreach (var hb in htmlBlockRanges)
            {
                AddToLineSet(excludedLines, hb.StartLine, hb.EndLine);
                if (hb.EndLine > hb.StartLine)
                    ranges.Add(new FoldRange(hb.StartLine, hb.EndLine));
            }

            // 4. Region markers
            ranges.AddRange(ParseRegionMarkers(lines, excludedLines));

            // 5. Blockquotes
            ranges.AddRange(ParseBlockquotes(lines, excludedLines));

            // 6. Lists
            ranges.AddRange(ParseLists(lines, excludedLines));

            // 7. Headings
            ranges.AddRange(ParseHeadings(lines, excludedLines));

            // 8. Tables
            ranges.AddRange(ParseTables(lines, excludedLines));

            return ranges;
        }

        private struct LineRange
        {
            public int StartLine;
            public int EndLine;
        }

        private static List<LineRange> ParseCodeBlocks(IReadOnlyList<string> lines)
        {
            var blocks = new List<LineRange>();
            int i = 0;

            while (i < lines.Count)
            {
                var openMatch = FenceOpen.Match(lines[i]);
                if (openMatch.Success)
                {
                    string fence = openMatch.Groups[2].Value;
                    int indent = openMatch.Groups[1].Length;
                    int startLine = i;
                    i++;

                    while (i < lines.Count)
                    {
                        var closeMatch = FenceOpen.Match(lines[i]);
                        if (closeMatch.Success && closeMatch.Groups[2].Value == fence && closeMatch.Groups[1].Length <= indent)
                        {
                            blocks.Add(new LineRange { StartLine = startLine, EndLine = i });
                            i++;
                            break;
                        }
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }

            return blocks;
        }

        private static HashSet<int> BuildLineSet(List<LineRange> ranges)
        {
            var set = new HashSet<int>();
            foreach (var r in ranges)
            {
                for (int i = r.StartLine; i <= r.EndLine; i++)
                    set.Add(i);
            }
            return set;
        }

        private static void AddToLineSet(HashSet<int> set, int startLine, int endLine)
        {
            for (int i = startLine; i <= endLine; i++)
                set.Add(i);
        }

        private static LineRange? ParseFrontMatter(IReadOnlyList<string> lines)
        {
            if (lines.Count < 2 || !FrontMatterDelimiter.IsMatch(lines[0]))
                return null;

            for (int i = 1; i < lines.Count; i++)
            {
                if (FrontMatterDelimiter.IsMatch(lines[i]))
                    return new LineRange { StartLine = 0, EndLine = i };
            }

            return null;
        }

        private static List<LineRange> ParseHtmlBlocks(IReadOnlyList<string> lines, HashSet<int> excludedLines)
        {
            var blocks = new List<LineRange>();
            int i = 0;

            while (i < lines.Count)
            {
                if (excludedLines.Contains(i))
                {
                    i++;
                    continue;
                }

                // HTML comment (but not region markers)
                var commentMatch = HtmlCommentOpen.Match(lines[i]);
                if (commentMatch.Success && !RegionStart.IsMatch(lines[i]) && !RegionEnd.IsMatch(lines[i]))
                {
                    if (lines[i].Contains("-->"))
                    {
                        i++;
                        continue;
                    }

                    int startLine = i;
                    i++;
                    while (i < lines.Count)
                    {
                        if (lines[i].Contains("-->"))
                        {
                            blocks.Add(new LineRange { StartLine = startLine, EndLine = i });
                            i++;
                            break;
                        }
                        i++;
                    }
                    continue;
                }

                // Block-level HTML element
                var htmlMatch = HtmlBlockOpen.Match(lines[i]);
                if (htmlMatch.Success)
                {
                    string tagName = htmlMatch.Groups[2].Value.ToLowerInvariant();
                    int startLine = i;

                    // Self-closing or single-line
                    var closeTagPattern = new Regex($"</{Regex.Escape(tagName)}\\s*>", RegexOptions.IgnoreCase);
                    if (closeTagPattern.IsMatch(lines[i]) || Regex.IsMatch(lines[i], @"/>\s*$"))
                    {
                        i++;
                        continue;
                    }

                    i++;
                    while (i < lines.Count)
                    {
                        if (closeTagPattern.IsMatch(lines[i]))
                        {
                            blocks.Add(new LineRange { StartLine = startLine, EndLine = i });
                            i++;
                            break;
                        }
                        if (lines[i].Trim().Length == 0)
                        {
                            if (i - 1 > startLine)
                                blocks.Add(new LineRange { StartLine = startLine, EndLine = i - 1 });
                            i++;
                            break;
                        }
                        i++;
                    }
                    continue;
                }

                i++;
            }

            return blocks;
        }

        private static List<FoldRange> ParseRegionMarkers(IReadOnlyList<string> lines, HashSet<int> excludedLines)
        {
            var ranges = new List<FoldRange>();
            var stack = new Stack<int>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (excludedLines.Contains(i))
                    continue;

                if (RegionStart.IsMatch(lines[i]))
                {
                    stack.Push(i);
                }
                else if (RegionEnd.IsMatch(lines[i]))
                {
                    if (stack.Count > 0)
                    {
                        int startLine = stack.Pop();
                        ranges.Add(new FoldRange(startLine, i, FoldKind.Region));
                    }
                }
            }

            return ranges;
        }

        private static List<FoldRange> ParseBlockquotes(IReadOnlyList<string> lines, HashSet<int> excludedLines)
        {
            var ranges = new List<FoldRange>();
            int i = 0;

            while (i < lines.Count)
            {
                if (excludedLines.Contains(i) || !BlockquoteLine.IsMatch(lines[i]))
                {
                    i++;
                    continue;
                }

                int startLine = i;
                i++;

                while (i < lines.Count && !excludedLines.Contains(i))
                {
                    if (BlockquoteLine.IsMatch(lines[i]))
                    {
                        i++;
                    }
                    else if (lines[i].Trim().Length == 0 && i + 1 < lines.Count && BlockquoteLine.IsMatch(lines[i + 1]))
                    {
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                int endLine = i - 1;
                while (endLine > startLine && lines[endLine].Trim().Length == 0)
                    endLine--;

                if (endLine > startLine)
                    ranges.Add(new FoldRange(startLine, endLine));
            }

            return ranges;
        }

        private static List<FoldRange> ParseLists(IReadOnlyList<string> lines, HashSet<int> excludedLines)
        {
            var ranges = new List<FoldRange>();
            var items = new List<(int Line, int Indent, int ContentIndent)>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (excludedLines.Contains(i))
                    continue;

                var match = ListItem.Match(lines[i]);
                if (match.Success)
                {
                    int indent = match.Groups[1].Length;
                    string marker = match.Groups[2].Value;
                    int contentIndent = indent + marker.Length + 1;
                    items.Add((i, indent, contentIndent));
                }
            }

            foreach (var current in items)
            {
                int endLine = current.Line;

                for (int i = current.Line + 1; i < lines.Count; i++)
                {
                    if (excludedLines.Contains(i))
                    {
                        endLine = i;
                        continue;
                    }

                    string trimmed = lines[i].Trim();

                    var nextItem = ListItem.Match(lines[i]);
                    if (nextItem.Success && nextItem.Groups[1].Length <= current.Indent)
                        break;

                    if (trimmed.Length != 0 && !nextItem.Success)
                    {
                        int lineIndent = lines[i].Length - lines[i].TrimStart().Length;
                        if (lineIndent < current.ContentIndent)
                            break;
                    }

                    if (trimmed.Length == 0)
                    {
                        int nextNonBlank = i + 1;
                        while (nextNonBlank < lines.Count && lines[nextNonBlank].Trim().Length == 0)
                            nextNonBlank++;

                        if (nextNonBlank >= lines.Count)
                            break;

                        int nextIndent = lines[nextNonBlank].Length - lines[nextNonBlank].TrimStart().Length;
                        bool nextIsListItem = ListItem.IsMatch(lines[nextNonBlank]);
                        if (nextIndent < current.ContentIndent && !(nextIsListItem && nextIndent > current.Indent))
                            break;
                    }

                    endLine = i;
                }

                while (endLine > current.Line && lines[endLine].Trim().Length == 0)
                    endLine--;

                if (endLine > current.Line)
                    ranges.Add(new FoldRange(current.Line, endLine));
            }

            return ranges;
        }

        private static List<FoldRange> ParseHeadings(IReadOnlyList<string> lines, HashSet<int> excludedLines)
        {
            var headings = new List<(int Line, int Level)>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (excludedLines.Contains(i))
                    continue;
                if (TabHeaderPattern.IsMatch(lines[i]))
                    continue;

                var match = Heading.Match(lines[i]);
                if (match.Success)
                    headings.Add((i, match.Groups[1].Length));
            }

            var ranges = new List<FoldRange>();

            for (int h = 0; h < headings.Count; h++)
            {
                var current = headings[h];
                int boundaryLine = lines.Count;

                for (int j = h + 1; j < headings.Count; j++)
                {
                    if (headings[j].Level <= current.Level)
                    {
                        boundaryLine = headings[j].Line;
                        break;
                    }
                }

                int endLine = boundaryLine - 1;
                while (endLine > current.Line && lines[endLine].Trim().Length == 0)
                    endLine--;

                if (endLine > current.Line)
                    ranges.Add(new FoldRange(current.Line, endLine));
            }

            return ranges;
        }

        private static List<FoldRange> ParseTables(IReadOnlyList<string> lines, HashSet<int> excludedLines)
        {
            var ranges = new List<FoldRange>();
            int i = 0;

            while (i < lines.Count)
            {
                if (excludedLines.Contains(i) || !TableRow.IsMatch(lines[i]))
                {
                    i++;
                    continue;
                }

                int tableStart = i;
                if (i + 1 < lines.Count && !excludedLines.Contains(i + 1) && TableSeparator.IsMatch(lines[i + 1]))
                {
                    i += 2;
                    while (i < lines.Count && !excludedLines.Contains(i) && TableRow.IsMatch(lines[i]))
                        i++;

                    int endLine = i - 1;
                    while (endLine > tableStart && lines[endLine].Trim().Length == 0)
                        endLine--;

                    if (endLine > tableStart)
                        ranges.Add(new FoldRange(tableStart, endLine));
                }
                else
                {
                    i++;
                }
            }

            return ranges;
        }
    }
}
