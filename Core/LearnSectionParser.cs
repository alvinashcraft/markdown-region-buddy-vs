using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace vs_md_extension_buddy.Core
{
    /// <summary>
    /// Parses Learn sections (monikers, zone pivots, tabs) from markdown lines.
    /// Pure C# with no Visual Studio SDK dependencies.
    /// </summary>
    public static class LearnSectionParser
    {
        private static readonly Regex MonikerStart = new Regex(@"^(\s*):::\s*moniker range=""([^""]+)""", RegexOptions.Compiled);
        private static readonly Regex MonikerEnd = new Regex(@"^(\s*):::\s*moniker-end", RegexOptions.Compiled);
        private static readonly Regex ZoneStart = new Regex(@"^(\s*):::\s*zone pivot=""([^""]+)""", RegexOptions.Compiled);
        private static readonly Regex ZoneEnd = new Regex(@"^(\s*):::\s*zone-end", RegexOptions.Compiled);
        private static readonly Regex TabHeader = new Regex(@"^(\s*)#{1,6} \[([^\]]+)\]\(#tab\/([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex TabEnd = new Regex(@"^(\s*)---\s*$", RegexOptions.Compiled);
        private static readonly Regex FenceOpen = new Regex(@"^(\s*)(```|~~~)", RegexOptions.Compiled);
        private static readonly Regex FrontMatterDelimiter = new Regex(@"^---\s*$", RegexOptions.Compiled);

        /// <summary>
        /// Parse all Learn sections in a document represented as a list of lines.
        /// </summary>
        public static List<LearnSection> ParseSections(IReadOnlyList<string> lines)
        {
            var sections = new List<LearnSection>();
            var codeFenceLines = BuildCodeFenceLineSet(lines);

            for (int i = 0; i < lines.Count; i++)
            {
                if (codeFenceLines.Contains(i))
                    continue;

                string line = lines[i];

                var monikerMatch = MonikerStart.Match(line);
                if (monikerMatch.Success)
                {
                    var section = ParseMonikerSection(lines, i, monikerMatch, codeFenceLines);
                    if (section != null)
                        sections.Add(section);
                }

                var zoneMatch = ZoneStart.Match(line);
                if (zoneMatch.Success)
                {
                    var section = ParseZoneSection(lines, i, zoneMatch, codeFenceLines);
                    if (section != null)
                        sections.Add(section);
                }

                var tabMatch = TabHeader.Match(line);
                if (tabMatch.Success)
                {
                    var section = ParseTabSection(lines, i, tabMatch, codeFenceLines);
                    if (section != null)
                        sections.Add(section);
                }
            }

            return sections;
        }

        /// <summary>
        /// Find the innermost section at a specific line.
        /// </summary>
        public static LearnSection FindSectionAtLine(IReadOnlyList<string> lines, int lineNumber)
        {
            return FindSectionAtLine(ParseSections(lines), lineNumber);
        }

        /// <summary>
        /// Find the innermost section at a specific line from pre-parsed sections.
        /// </summary>
        public static LearnSection FindSectionAtLine(List<LearnSection> sections, int lineNumber)
        {
            var containing = sections
                .Where(s => lineNumber >= s.StartLine && lineNumber <= s.EndLine)
                .ToList();

            if (containing.Count == 0)
                return null;

            return containing.Aggregate((innermost, current) =>
            {
                int innermostSize = innermost.EndLine - innermost.StartLine;
                int currentSize = current.EndLine - current.StartLine;
                return currentSize < innermostSize ? current : innermost;
            });
        }

        /// <summary>
        /// Find all sections with a specific type and name.
        /// </summary>
        public static List<LearnSection> FindSectionsByName(IReadOnlyList<string> lines, SectionType type, string name)
        {
            return FindSectionsByName(ParseSections(lines), type, name);
        }

        /// <summary>
        /// Find all sections with a specific type and name from pre-parsed sections.
        /// </summary>
        public static List<LearnSection> FindSectionsByName(List<LearnSection> sections, SectionType type, string name)
        {
            return sections
                .Where(s => s.Type == type && s.Name == name)
                .ToList();
        }

        /// <summary>
        /// Get all unique section type/name combinations in the document.
        /// </summary>
        public static List<(SectionType Type, string Name, string Label)> GetUniqueSections(IReadOnlyList<string> lines)
        {
            return GetUniqueSections(ParseSections(lines));
        }

        /// <summary>
        /// Get all unique section type/name combinations from pre-parsed sections.
        /// </summary>
        public static List<(SectionType Type, string Name, string Label)> GetUniqueSections(List<LearnSection> sections)
        {
            var uniqueMap = new Dictionary<string, (SectionType Type, string Name, string Label)>();

            foreach (var section in sections)
            {
                string key = $"{section.Type}:{section.Name}";
                if (!uniqueMap.ContainsKey(key))
                {
                    string typeLabel = section.Type.ToString().ToLowerInvariant();
                    uniqueMap[key] = (section.Type, section.Name, $"{typeLabel}: {section.Name}");
                }
            }

            return uniqueMap.Values.ToList();
        }

        /// <summary>
        /// Build a set of line numbers that fall inside fenced code blocks or front matter.
        /// </summary>
        private static HashSet<int> BuildCodeFenceLineSet(IReadOnlyList<string> lines)
        {
            var set = new HashSet<int>();

            // Detect front matter at line 0
            if (lines.Count >= 2 && FrontMatterDelimiter.IsMatch(lines[0]))
            {
                for (int i = 1; i < lines.Count; i++)
                {
                    if (FrontMatterDelimiter.IsMatch(lines[i]))
                    {
                        for (int j = 0; j <= i; j++)
                            set.Add(j);
                        break;
                    }
                }
            }

            // Detect fenced code blocks
            int idx = 0;
            while (idx < lines.Count)
            {
                if (set.Contains(idx))
                {
                    idx++;
                    continue;
                }

                var openMatch = FenceOpen.Match(lines[idx]);
                if (openMatch.Success)
                {
                    string fence = openMatch.Groups[2].Value;
                    int indent = openMatch.Groups[1].Length;
                    int startLine = idx;
                    idx++;

                    while (idx < lines.Count)
                    {
                        var closeMatch = FenceOpen.Match(lines[idx]);
                        if (closeMatch.Success && closeMatch.Groups[2].Value == fence && closeMatch.Groups[1].Length <= indent)
                        {
                            for (int j = startLine; j <= idx; j++)
                                set.Add(j);
                            idx++;
                            break;
                        }
                        idx++;
                    }
                }
                else
                {
                    idx++;
                }
            }

            return set;
        }

        private static LearnSection ParseMonikerSection(IReadOnlyList<string> lines, int startLine, Match match, HashSet<int> codeFenceLines)
        {
            string indent = match.Groups[1].Value;
            string name = match.Groups[2].Value;
            int indentLevel = indent.Length;

            for (int i = startLine + 1; i < lines.Count; i++)
            {
                if (codeFenceLines.Contains(i))
                    continue;

                var endMatch = MonikerEnd.Match(lines[i]);
                if (endMatch.Success && endMatch.Groups[1].Length == indentLevel)
                {
                    return new LearnSection(SectionType.Moniker, name, startLine, i, indentLevel);
                }
            }

            return null;
        }

        private static LearnSection ParseZoneSection(IReadOnlyList<string> lines, int startLine, Match match, HashSet<int> codeFenceLines)
        {
            string indent = match.Groups[1].Value;
            string name = match.Groups[2].Value;
            int indentLevel = indent.Length;

            for (int i = startLine + 1; i < lines.Count; i++)
            {
                if (codeFenceLines.Contains(i))
                    continue;

                var endMatch = ZoneEnd.Match(lines[i]);
                if (endMatch.Success && endMatch.Groups[1].Length == indentLevel)
                {
                    return new LearnSection(SectionType.Zone, name, startLine, i, indentLevel);
                }
            }

            return null;
        }

        private static LearnSection ParseTabSection(IReadOnlyList<string> lines, int startLine, Match match, HashSet<int> codeFenceLines)
        {
            string indent = match.Groups[1].Value;
            string tabId = match.Groups[3].Value;
            int indentLevel = indent.Length;

            for (int i = startLine + 1; i < lines.Count; i++)
            {
                if (codeFenceLines.Contains(i))
                    continue;

                string line = lines[i];

                // Check for horizontal rule (end of tab group)
                if (TabEnd.IsMatch(line))
                {
                    return new LearnSection(SectionType.Tab, tabId, startLine, i, indentLevel);
                }

                // Check for next tab header (end of current tab)
                var nextTabMatch = TabHeader.Match(line);
                if (nextTabMatch.Success && nextTabMatch.Groups[1].Length == indentLevel)
                {
                    return new LearnSection(SectionType.Tab, tabId, startLine, i - 1, indentLevel);
                }
            }

            // Reached end of document
            return new LearnSection(SectionType.Tab, tabId, startLine, lines.Count - 1, indentLevel);
        }
    }
}
