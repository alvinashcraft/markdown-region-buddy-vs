namespace vs_md_extension_buddy.Core
{
    /// <summary>
    /// A simple line-range pair representing a foldable region in a markdown document.
    /// </summary>
    public struct FoldRange
    {
        public int StartLine { get; }
        public int EndLine { get; }
        public FoldKind Kind { get; }

        public FoldRange(int startLine, int endLine, FoldKind kind = FoldKind.None)
        {
            StartLine = startLine;
            EndLine = endLine;
            Kind = kind;
        }
    }

    public enum FoldKind
    {
        None,
        Region
    }
}
