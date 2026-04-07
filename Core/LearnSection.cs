namespace vs_md_extension_buddy.Core
{
    /// <summary>
    /// Represents a Learn section (moniker, zone pivot, or tab) in a markdown document.
    /// </summary>
    public class LearnSection
    {
        public SectionType Type { get; }
        public string Name { get; }
        public int StartLine { get; }
        public int EndLine { get; }
        public int IndentLevel { get; }

        public LearnSection(SectionType type, string name, int startLine, int endLine, int indentLevel)
        {
            Type = type;
            Name = name;
            StartLine = startLine;
            EndLine = endLine;
            IndentLevel = indentLevel;
        }
    }
}
