using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using vs_md_extension_buddy.Core;

namespace vs_md_extension_buddy
{
    #region Adornment layer definition

    internal static class LearnAdornmentLayerDefinition
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(LearnAdornmentManager.LayerName)]
        [Order(Before = PredefinedAdornmentLayers.Selection)]
        internal static AdornmentLayerDefinition LayerDefinition = null;
    }

    #endregion

    #region Provider

    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("vs-markdown")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class LearnAdornmentManagerProvider : IWpfTextViewCreationListener
    {
        public void TextViewCreated(IWpfTextView textView)
        {
            textView.Properties.GetOrCreateSingletonProperty(
                typeof(LearnAdornmentManager),
                () => new LearnAdornmentManager(textView));
        }
    }

    #endregion

    /// <summary>
    /// Draws translucent background rectangles behind Learn sections
    /// (moniker=blue, zone=green, tab=pink) on an adornment layer.
    /// </summary>
    internal sealed class LearnAdornmentManager : IDisposable
    {
        internal const string LayerName = "LearnSectionHighlight";

        private static readonly Dictionary<SectionType, Color> SectionColors = new Dictionary<SectionType, Color>
        {
            { SectionType.Moniker, Color.FromRgb(100, 149, 237) }, // Cornflower blue
            { SectionType.Zone,    Color.FromRgb(60, 179, 113) },  // Medium sea green
            { SectionType.Tab,     Color.FromRgb(205, 92, 92) },   // Indian red
        };

        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private bool _disposed;

        public LearnAdornmentManager(IWpfTextView view)
        {
            _view = view;
            _layer = view.GetAdornmentLayer(LayerName);

            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnViewClosed;
            LearnOptionPage.SettingsChanged += OnSettingsChanged;

            UpdateAdornments();
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            UpdateAdornments();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            UpdateAdornments();
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void UpdateAdornments()
        {
            if (_disposed) return;
            if (_view.TextViewLines == null) return;

            _layer.RemoveAllAdornments();

            if (!IsEnabled())
                return;

            double opacity = GetOpacity();
            var snapshot = _view.TextSnapshot;
            var lines = GetLines(snapshot);
            var sections = LearnSectionParser.ParseSections(lines);

            foreach (var section in sections)
            {
                if (!SectionColors.TryGetValue(section.Type, out var color))
                    continue;

                DrawSectionBackground(snapshot, section, color, opacity);
            }
        }

        private void DrawSectionBackground(
            ITextSnapshot snapshot, LearnSection section, Color color, double opacity)
        {
            for (int i = section.StartLine; i <= section.EndLine && i < snapshot.LineCount; i++)
            {
                var line = _view.TextViewLines.GetTextViewLineContainingBufferPosition(
                    snapshot.GetLineFromLineNumber(i).Start);

                if (line == null || line.VisibilityState == Microsoft.VisualStudio.Text.Formatting.VisibilityState.Unattached)
                    continue;

                var rect = new Rectangle
                {
                    Width = Math.Max(_view.ViewportWidth, line.Width),
                    Height = line.Height,
                    Fill = new SolidColorBrush(color),
                    Opacity = opacity,
                    IsHitTestVisible = false,
                };

                Canvas.SetLeft(rect, _view.ViewportLeft);
                Canvas.SetTop(rect, line.Top);

                _layer.AddAdornment(
                    AdornmentPositioningBehavior.TextRelative,
                    line.Extent,
                    null,
                    rect,
                    null);
            }
        }

        private static bool IsEnabled()
        {
            var package = vs_md_extension_buddyPackage.Instance;
            if (package == null) return false;

            var page = (LearnOptionPage)package.GetDialogPage(typeof(LearnOptionPage));
            return page?.EnableDecorations ?? false;
        }

        private static double GetOpacity()
        {
            var package = vs_md_extension_buddyPackage.Instance;
            if (package == null) return 0.05;

            var page = (LearnOptionPage)package.GetDialogPage(typeof(LearnOptionPage));
            double val = page?.DecorationOpacity ?? 0.05;
            return Math.Max(0.01, Math.Min(0.3, val));
        }

        private static IReadOnlyList<string> GetLines(ITextSnapshot snapshot)
        {
            var lines = new List<string>(snapshot.LineCount);
            for (int i = 0; i < snapshot.LineCount; i++)
                lines.Add(snapshot.GetLineFromLineNumber(i).GetText());
            return lines;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _view.LayoutChanged -= OnLayoutChanged;
                _view.Closed -= OnViewClosed;
                LearnOptionPage.SettingsChanged -= OnSettingsChanged;
                _layer.RemoveAllAdornments();
            }
        }
    }
}
