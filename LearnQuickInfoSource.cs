using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;
using vs_md_extension_buddy.Core;

namespace vs_md_extension_buddy
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Learn QuickInfo Source")]
    [ContentType("vs-markdown")]
    [Order]
    internal sealed class LearnQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                typeof(LearnQuickInfoSource),
                () => new LearnQuickInfoSource(buffer, OutliningManagerService));
        }
    }

    /// <summary>
    /// Shows a preview of collapsed Learn sections (up to 20 lines) when
    /// the user hovers over the start line of a collapsed region.
    /// </summary>
    internal sealed class LearnQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer _buffer;
        private readonly IOutliningManagerService _outliningManagerService;
        private const int MaxPreviewLines = 20;

        public LearnQuickInfoSource(ITextBuffer buffer, IOutliningManagerService outliningManagerService)
        {
            _buffer = buffer;
            _outliningManagerService = outliningManagerService;
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session,
            CancellationToken cancellationToken)
        {
            var triggerPoint = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!triggerPoint.HasValue)
                return Task.FromResult<QuickInfoItem>(null);

            var snapshot = _buffer.CurrentSnapshot;
            int lineNumber = triggerPoint.Value.GetContainingLine().LineNumber;

            var lines = GetLines(snapshot);
            var section = LearnSectionParser.FindSectionAtLine(lines, lineNumber);

            // Only show hover on the start line
            if (section == null || lineNumber != section.StartLine)
                return Task.FromResult<QuickInfoItem>(null);

            // Check if the section is collapsed
            var manager = _outliningManagerService.GetOutliningManager(session.TextView);
            if (manager == null)
                return Task.FromResult<QuickInfoItem>(null);

            bool isCollapsed = IsSectionCollapsed(manager, snapshot, section);
            if (!isCollapsed)
                return Task.FromResult<QuickInfoItem>(null);

            // Build preview content
            string typeLabel = GetTypeLabel(section.Type);
            string previewText = BuildPreview(lines, section);

            var content = new ContainerElement(
                ContainerElementStyle.Stacked,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Keyword,
                        $"{typeLabel}: {section.Name}",
                        ClassifiedTextRunStyle.Bold)),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.String,
                        previewText)));

            var line = triggerPoint.Value.GetContainingLine();
            var applicableSpan = snapshot.CreateTrackingSpan(
                line.Extent, SpanTrackingMode.EdgeInclusive);

            return Task.FromResult(new QuickInfoItem(applicableSpan, content));
        }

        private static bool IsSectionCollapsed(
            IOutliningManager manager, ITextSnapshot snapshot, LearnSection section)
        {
            var wholeDoc = new SnapshotSpan(snapshot, 0, snapshot.Length);
            var collapsedRegions = manager.GetCollapsedRegions(wholeDoc);

            return collapsedRegions.Any(r =>
            {
                var span = r.Extent.GetSpan(snapshot);
                return snapshot.GetLineNumberFromPosition(span.Start) == section.StartLine;
            });
        }

        private static string GetTypeLabel(SectionType type)
        {
            switch (type)
            {
                case SectionType.Moniker: return "Moniker Range";
                case SectionType.Zone: return "Zone Pivot";
                case SectionType.Tab: return "Tab";
                default: return type.ToString();
            }
        }

        private static string BuildPreview(System.Collections.Generic.IReadOnlyList<string> lines, LearnSection section)
        {
            int totalLines = section.EndLine - section.StartLine + 1;
            int linesToShow = Math.Min(totalLines, MaxPreviewLines);
            var previewLines = new System.Collections.Generic.List<string>(linesToShow + 1);

            for (int i = section.StartLine; i < section.StartLine + linesToShow && i <= section.EndLine && i < lines.Count; i++)
            {
                previewLines.Add(lines[i]);
            }

            if (totalLines > MaxPreviewLines)
                previewLines.Add($"... ({totalLines - MaxPreviewLines} more lines)");

            return string.Join(Environment.NewLine, previewLines);
        }

        private static System.Collections.Generic.IReadOnlyList<string> GetLines(ITextSnapshot snapshot)
        {
            var lines = new System.Collections.Generic.List<string>(snapshot.LineCount);
            for (int i = 0; i < snapshot.LineCount; i++)
                lines.Add(snapshot.GetLineFromLineNumber(i).GetText());
            return lines;
        }

        public void Dispose() { }
    }
}
