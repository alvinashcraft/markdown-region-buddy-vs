using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using vs_md_extension_buddy.Core;

namespace vs_md_extension_buddy
{
    internal sealed class LearnFoldingCommands
    {
        public static readonly Guid CommandSetGuid = new Guid("d4c3b2a1-e5f6-4a7b-8c9d-0e1f2a3b4c5d");

        private const int CmdToggleCurrentSection   = 0x0100;
        private const int CmdExpandCurrentSection   = 0x0101;
        private const int CmdCollapseCurrentSection = 0x0102;
        private const int CmdExpandAllRegions       = 0x0103;
        private const int CmdCollapseAllRegions     = 0x0104;
        private const int CmdExpandNamedSection     = 0x0105;
        private const int CmdCollapseNamedSection   = 0x0106;
        private const int CmdFocusSection           = 0x0107;
        private const int CmdToggleDecorations     = 0x0108;

        private readonly AsyncPackage _package;
        private readonly IOutliningManagerService _outliningManagerService;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersService;

        private LearnFoldingCommands(
            AsyncPackage package,
            OleMenuCommandService commandService,
            IOutliningManagerService outliningManagerService,
            IVsEditorAdaptersFactoryService editorAdaptersService)
        {
            _package = package;
            _outliningManagerService = outliningManagerService;
            _editorAdaptersService = editorAdaptersService;

            RegisterCommand(commandService, CmdToggleCurrentSection, ExecuteToggleCurrent);
            RegisterCommand(commandService, CmdExpandCurrentSection, ExecuteExpandCurrent);
            RegisterCommand(commandService, CmdCollapseCurrentSection, ExecuteCollapseCurrent);
            RegisterCommand(commandService, CmdExpandAllRegions, ExecuteExpandAll);
            RegisterCommand(commandService, CmdCollapseAllRegions, ExecuteCollapseAll);
            RegisterCommand(commandService, CmdExpandNamedSection, ExecuteExpandNamed);
            RegisterCommand(commandService, CmdCollapseNamedSection, ExecuteCollapseNamed);
            RegisterCommand(commandService, CmdFocusSection, ExecuteFocus);
            RegisterCommand(commandService, CmdToggleDecorations, ExecuteToggleDecorations);
        }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null) return;

            var componentModel = await package.GetServiceAsync(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel))
                as Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
            if (componentModel == null) return;

            var outliningManagerService = componentModel.GetService<IOutliningManagerService>();
            var editorAdaptersService = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            new LearnFoldingCommands(package, commandService, outliningManagerService, editorAdaptersService);
        }

        private void RegisterCommand(OleMenuCommandService commandService, int commandId, EventHandler handler)
        {
            var menuCommandId = new CommandID(CommandSetGuid, commandId);
            var command = new OleMenuCommand(handler, menuCommandId);
            command.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand command)
            {
                var view = GetActiveTextView();
                command.Visible = view != null && IsMarkdownBuffer(view.TextBuffer);
            }
        }

        #region Command handlers

        private void ExecuteToggleCurrent(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var (view, manager, section) = GetCurrentSection();
            if (section == null) return;

            var region = FindRegionForSection(manager, view.TextSnapshot, section);
            if (region == null) return;

            if (region.IsCollapsed)
                manager.Expand(region as ICollapsed);
            else
                manager.TryCollapse(region);
        }

        private void ExecuteExpandCurrent(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var (view, manager, section) = GetCurrentSection();
            if (section == null) return;

            var region = FindRegionForSection(manager, view.TextSnapshot, section);
            if (region is ICollapsed collapsed)
                manager.Expand(collapsed);
        }

        private void ExecuteCollapseCurrent(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var (view, manager, section) = GetCurrentSection();
            if (section == null) return;

            var region = FindRegionForSection(manager, view.TextSnapshot, section);
            if (region != null && !region.IsCollapsed)
                manager.TryCollapse(region);
        }

        private void ExecuteExpandAll(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var (view, manager) = GetViewAndManager();
            if (view == null) return;

            var snapshot = view.TextSnapshot;
            var lines = GetLines(snapshot);
            var sections = LearnSectionParser.ParseSections(lines);

            foreach (var section in sections)
            {
                var region = FindRegionForSection(manager, snapshot, section);
                if (region is ICollapsed collapsed)
                    manager.Expand(collapsed);
            }
        }

        private void ExecuteCollapseAll(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var (view, manager) = GetViewAndManager();
            if (view == null) return;

            var snapshot = view.TextSnapshot;
            var lines = GetLines(snapshot);
            var sections = LearnSectionParser.ParseSections(lines);

            foreach (var section in sections)
            {
                var region = FindRegionForSection(manager, snapshot, section);
                if (region != null && !region.IsCollapsed)
                    manager.TryCollapse(region);
            }
        }

        private void ExecuteExpandNamed(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var (view, manager, section) = GetCurrentSection();
            if (section == null) return;

            var snapshot = view.TextSnapshot;
            var lines = GetLines(snapshot);
            var matching = LearnSectionParser.FindSectionsByName(lines, section.Type, section.Name);

            int count = 0;
            foreach (var s in matching)
            {
                var region = FindRegionForSection(manager, snapshot, s);
                if (region is ICollapsed collapsed)
                {
                    manager.Expand(collapsed);
                    count++;
                }
            }

            ShowStatusMessage($"Expanded {matching.Count} section(s): {section.Type.ToString().ToLowerInvariant()} \"{section.Name}\"");
        }

        private void ExecuteCollapseNamed(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var (view, manager, section) = GetCurrentSection();
            if (section == null) return;

            var snapshot = view.TextSnapshot;
            var lines = GetLines(snapshot);
            var matching = LearnSectionParser.FindSectionsByName(lines, section.Type, section.Name);

            foreach (var s in matching)
            {
                var region = FindRegionForSection(manager, snapshot, s);
                if (region != null && !region.IsCollapsed)
                    manager.TryCollapse(region);
            }

            ShowStatusMessage($"Collapsed {matching.Count} section(s): {section.Type.ToString().ToLowerInvariant()} \"{section.Name}\"");
        }

        private void ExecuteFocus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var (view, manager) = GetViewAndManager();
            if (view == null) return;

            var snapshot = view.TextSnapshot;
            var lines = GetLines(snapshot);
            var uniqueSections = LearnSectionParser.GetUniqueSections(lines);

            if (uniqueSections.Count == 0)
            {
                ShowStatusMessage("No regions found in this document.");
                return;
            }

            // Show a picker dialog
            var selected = ShowSectionPicker(uniqueSections);
            if (selected == null || selected.Count == 0)
                return;

            // Build selected names per type
            var selectedByType = new Dictionary<SectionType, HashSet<string>>();
            foreach (var item in selected)
            {
                if (!selectedByType.ContainsKey(item.Type))
                    selectedByType[item.Type] = new HashSet<string>();
                selectedByType[item.Type].Add(item.Name);
            }

            // Auto-include compound zone pivots
            if (selectedByType.ContainsKey(SectionType.Zone))
            {
                var selectedZoneNames = selectedByType[SectionType.Zone];
                foreach (var zoneSection in uniqueSections.Where(s => s.Type == SectionType.Zone))
                {
                    if (selectedZoneNames.Contains(zoneSection.Name))
                        continue;
                    var parts = zoneSection.Name.Split(',').Select(p => p.Trim());
                    if (parts.Any(p => selectedZoneNames.Contains(p)))
                        selectedZoneNames.Add(zoneSection.Name);
                }
            }

            var allSections = LearnSectionParser.ParseSections(lines);

            // Collapse all sections of selected types, then expand matches
            foreach (var section in allSections)
            {
                if (!selectedByType.ContainsKey(section.Type))
                    continue;

                var region = FindRegionForSection(manager, snapshot, section);
                if (region != null && !region.IsCollapsed)
                    manager.TryCollapse(region);
            }

            int expandedCount = 0;

            // Re-query after collapse since region state changed
            foreach (var section in allSections)
            {
                if (!selectedByType.ContainsKey(section.Type))
                    continue;

                var names = selectedByType[section.Type];
                if (!names.Contains(section.Name))
                    continue;

                var region = FindRegionForSection(manager, snapshot, section);
                if (region is ICollapsed collapsed)
                {
                    manager.Expand(collapsed);
                    expandedCount++;
                }
            }

            var labels = string.Join(", ", selected.Select(s => s.Label));
            ShowStatusMessage($"Focused on: {labels} ({expandedCount} section(s))");
        }

        private void ExecuteToggleDecorations(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var package = vs_md_extension_buddyPackage.Instance;
            if (package == null) return;

            var page = (LearnOptionPage)package.GetDialogPage(typeof(LearnOptionPage));
            if (page == null) return;

            page.EnableDecorations = !page.EnableDecorations;
            page.SaveSettingsToStorage();
            LearnOptionPage.RaiseSettingsChanged();

            ShowStatusMessage($"Region decorations {(page.EnableDecorations ? "enabled" : "disabled")}.");
        }

        #endregion

        #region Helpers

        private IWpfTextView GetActiveTextView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textManager = _package.GetService<SVsTextManager, IVsTextManager>();
            if (textManager == null) return null;

            textManager.GetActiveView(1, null, out IVsTextView vsTextView);
            if (vsTextView == null) return null;

            return _editorAdaptersService.GetWpfTextView(vsTextView);
        }

        private (IWpfTextView view, IOutliningManager manager) GetViewAndManager()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var view = GetActiveTextView();
            if (view == null) return (null, null);

            if (!IsMarkdownBuffer(view.TextBuffer))
                return (null, null);

            var manager = _outliningManagerService.GetOutliningManager(view);
            return (view, manager);
        }

        private (IWpfTextView view, IOutliningManager manager, LearnSection section) GetCurrentSection()
        {
            var (view, manager) = GetViewAndManager();
            if (view == null)
                return (null, null, null);

            var snapshot = view.TextSnapshot;
            int caretLine = view.Caret.Position.BufferPosition.GetContainingLine().LineNumber;
            var lines = GetLines(snapshot);
            var section = LearnSectionParser.FindSectionAtLine(lines, caretLine);

            if (section == null)
            {
                ShowStatusMessage("No region found at cursor position.");
                return (view, manager, null);
            }

            return (view, manager, section);
        }

        private static ICollapsible FindRegionForSection(
            IOutliningManager manager, ITextSnapshot snapshot, LearnSection section)
        {
            if (manager == null || section.StartLine >= snapshot.LineCount)
                return null;

            var wholeDoc = new SnapshotSpan(snapshot, 0, snapshot.Length);
            var allRegions = manager.GetAllRegions(wholeDoc);

            foreach (var region in allRegions)
            {
                var span = region.Extent.GetSpan(snapshot);
                int regionStartLine = snapshot.GetLineNumberFromPosition(span.Start);
                if (regionStartLine == section.StartLine)
                    return region;
            }

            return null;
        }

        private static IReadOnlyList<string> GetLines(ITextSnapshot snapshot)
        {
            var lines = new List<string>(snapshot.LineCount);
            for (int i = 0; i < snapshot.LineCount; i++)
                lines.Add(snapshot.GetLineFromLineNumber(i).GetText());
            return lines;
        }

        private static bool IsMarkdownBuffer(ITextBuffer buffer)
        {
            var ct = buffer.ContentType;
            return ct.IsOfType("vs-markdown") || ct.IsOfType("markdown");
        }

        private void ShowStatusMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var statusBar = _package.GetService<SVsStatusbar, IVsStatusbar>();
            statusBar?.SetText(message);
        }

        /// <summary>
        /// Shows a dialog for selecting sections to focus on.
        /// </summary>
        private static List<(SectionType Type, string Name, string Label)> ShowSectionPicker(
            List<(SectionType Type, string Name, string Label)> sections)
        {
            var listBox = new ListBox
            {
                SelectionMode = SelectionMode.Extended,
                Margin = new Thickness(8),
                MinHeight = 120,
            };

            foreach (var s in sections)
                listBox.Items.Add(s.Label);

            if (listBox.Items.Count > 0)
                listBox.SelectedIndex = 0;

            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(4), IsDefault = true };
            var cancelButton = new Button { Content = "Cancel", Width = 75, Margin = new Thickness(4), IsCancel = true };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 8, 8),
            };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            var panel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Bottom);
            panel.Children.Add(buttonPanel);
            panel.Children.Add(listBox);

            var dialog = new Microsoft.VisualStudio.PlatformUI.DialogWindow("MarkdownRegionBuddy.FocusSection")
            {
                Title = "Focus on Section",
                Width = 350,
                Height = 280,
                Content = panel,
                ResizeMode = ResizeMode.CanResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            bool confirmed = false;
            okButton.Click += (s, e) => { confirmed = true; dialog.Close(); };

            dialog.ShowModal();

            if (!confirmed)
                return null;

            var result = new List<(SectionType Type, string Name, string Label)>();
            foreach (var item in listBox.SelectedItems)
            {
                string label = item as string;
                int index = listBox.Items.IndexOf(label);
                if (index >= 0 && index < sections.Count)
                    result.Add(sections[index]);
            }

            return result;
        }

        #endregion
    }
}
