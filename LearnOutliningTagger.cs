using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using vs_md_extension_buddy.Core;

namespace vs_md_extension_buddy
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("vs-markdown")]
    internal sealed class LearnOutliningTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null || typeof(T) != typeof(IOutliningRegionTag))
                return null;

            return buffer.Properties.GetOrCreateSingletonProperty(
                typeof(LearnOutliningTagger),
                () => new LearnOutliningTagger(buffer)) as ITagger<T>;
        }
    }

    /// <summary>
    /// Provides outlining regions for Learn markdown sections (monikers, zone pivots, tabs)
    /// AND standard markdown elements (headings, tables, front matter, blockquotes, HTML blocks,
    /// region markers, lists). Standard markdown folds that overlap Learn sections are clipped
    /// to prevent invalid overlapping ranges.
    /// </summary>
    internal sealed class LearnOutliningTagger : ITagger<IOutliningRegionTag>, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private Timer _debounceTimer;
        private bool _disposed;

        private const int DebounceMs = 300;
        private const int MaxHintLines = 20;

        private sealed class ParseState
        {
            public static readonly ParseState Empty = new ParseState(null, Array.Empty<OutlineRegion>());

            public readonly ITextSnapshot Snapshot;
            public readonly IReadOnlyList<OutlineRegion> Regions;

            public ParseState(ITextSnapshot snapshot, IReadOnlyList<OutlineRegion> regions)
            {
                Snapshot = snapshot;
                Regions = regions;
            }
        }

        private struct OutlineRegion
        {
            public int StartLine;
            public int EndLine;
            public string HintText;
            public bool IsRegionKind;
        }

        private volatile ParseState _state = ParseState.Empty;

        public LearnOutliningTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.Changed += OnBufferChanged;
            Parse(_buffer.CurrentSnapshot);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (_disposed) return;
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ => Parse(_buffer.CurrentSnapshot), null, DebounceMs, Timeout.Infinite);
        }

        private void Parse(ITextSnapshot snapshot)
        {
            if (_disposed) return;

            var lines = GetLines(snapshot);
            var sections = LearnSectionParser.ParseSections(lines);
            var markdownFolds = MarkdownFoldingHelper.GetFoldingRanges(lines);

            var newRegions = new List<OutlineRegion>();

            // Learn section regions
            foreach (var section in sections)
            {
                if (section.EndLine <= section.StartLine)
                    continue;

                // End one line before the end marker so it stays visible
                int foldEnd = section.EndLine - 1;
                if (foldEnd <= section.StartLine)
                    continue;

                newRegions.Add(new OutlineRegion
                {
                    StartLine = section.StartLine,
                    EndLine = foldEnd,
                    HintText = BuildHintText(lines, section.StartLine, section.EndLine),
                    IsRegionKind = false,
                });
            }

            // Standard markdown folds — clipped to avoid overlapping Learn sections
            var clippedFolds = ClipMarkdownFolds(markdownFolds, sections);
            foreach (var fold in clippedFolds)
            {
                if (fold.EndLine <= fold.StartLine)
                    continue;

                newRegions.Add(new OutlineRegion
                {
                    StartLine = fold.StartLine,
                    EndLine = fold.EndLine,
                    HintText = BuildHintText(lines, fold.StartLine, fold.EndLine),
                    IsRegionKind = fold.Kind == FoldKind.Region,
                });
            }

            _state = new ParseState(snapshot, newRegions);

            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            var state = _state;
            if (state.Snapshot == null || state.Regions.Count == 0)
                yield break;

            var requestedSnapshot = spans[0].Snapshot;

            foreach (var region in state.Regions)
            {
                if (region.StartLine >= state.Snapshot.LineCount ||
                    region.EndLine >= state.Snapshot.LineCount)
                    continue;

                var startLine = state.Snapshot.GetLineFromLineNumber(region.StartLine);
                var endLine = state.Snapshot.GetLineFromLineNumber(region.EndLine);

                var regionSpan = new SnapshotSpan(startLine.End, endLine.End);
                if (state.Snapshot != requestedSnapshot)
                {
                    regionSpan = regionSpan.TranslateTo(requestedSnapshot, SpanTrackingMode.EdgeExclusive);
                }

                if (regionSpan.IsEmpty)
                    continue;

                yield return new TagSpan<IOutliningRegionTag>(
                    regionSpan,
                    new OutliningRegionTag(
                        isDefaultCollapsed: false,
                        isImplementation: region.IsRegionKind,
                        collapsedForm: "...",
                        collapsedHintForm: region.HintText));
            }
        }

        #region Clip logic

        /// <summary>
        /// Clip standard markdown folds that overlap Learn section boundaries.
        /// Two cases:
        ///   1. Fold starts INSIDE a section and extends past its end -> clip to section end - 1.
        ///   2. Fold starts OUTSIDE and partially overlaps a section -> clip to section start - 1.
        /// </summary>
        private static List<FoldRange> ClipMarkdownFolds(
            List<FoldRange> markdownFolds, List<LearnSection> sections)
        {
            var result = new List<FoldRange>();

            foreach (var fold in markdownFolds)
            {
                var container = FindContainingSection(sections, fold.StartLine);
                if (container != null)
                {
                    if (fold.EndLine > container.EndLine)
                    {
                        int clippedEnd = container.EndLine - 1;
                        if (clippedEnd <= fold.StartLine)
                            continue;
                        result.Add(new FoldRange(fold.StartLine, clippedEnd, fold.Kind));
                        continue;
                    }
                }
                else
                {
                    var overlapped = FindFirstPartiallyOverlappedSection(
                        sections, fold.StartLine, fold.EndLine);
                    if (overlapped != null)
                    {
                        int clippedEnd = overlapped.StartLine - 1;
                        if (clippedEnd <= fold.StartLine)
                            continue;
                        result.Add(new FoldRange(fold.StartLine, clippedEnd, fold.Kind));
                        continue;
                    }
                }

                result.Add(fold);
            }

            return result;
        }

        private static LearnSection FindContainingSection(List<LearnSection> sections, int line)
        {
            LearnSection best = null;
            foreach (var section in sections)
            {
                if (line > section.StartLine && line < section.EndLine)
                {
                    if (best == null || (section.EndLine - section.StartLine) < (best.EndLine - best.StartLine))
                        best = section;
                }
            }
            return best;
        }

        private static LearnSection FindFirstPartiallyOverlappedSection(
            List<LearnSection> sections, int rangeStart, int rangeEnd)
        {
            LearnSection first = null;
            foreach (var section in sections)
            {
                if (section.StartLine > rangeStart &&
                    section.StartLine <= rangeEnd &&
                    rangeEnd < section.EndLine)
                {
                    if (first == null || section.StartLine < first.StartLine)
                        first = section;
                }
            }
            return first;
        }

        #endregion

        #region Helpers

        private static string BuildHintText(IReadOnlyList<string> lines, int startLine, int endLine)
        {
            int totalLines = endLine - startLine + 1;
            int linesToShow = Math.Min(totalLines, MaxHintLines);
            var hintLines = new List<string>(linesToShow + 1);

            for (int i = startLine; i < startLine + linesToShow && i <= endLine && i < lines.Count; i++)
            {
                hintLines.Add(lines[i]);
            }

            if (totalLines > MaxHintLines)
                hintLines.Add($"... ({totalLines - MaxHintLines} more lines)");

            return string.Join(Environment.NewLine, hintLines);
        }

        private static IReadOnlyList<string> GetLines(ITextSnapshot snapshot)
        {
            var lines = new List<string>(snapshot.LineCount);
            for (int i = 0; i < snapshot.LineCount; i++)
                lines.Add(snapshot.GetLineFromLineNumber(i).GetText());
            return lines;
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _debounceTimer?.Dispose();
                _buffer.Changed -= OnBufferChanged;
            }
        }
    }
}
