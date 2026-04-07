using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace vs_md_extension_buddy
{
    /// <summary>
    /// Options page for Markdown Region Buddy, accessible via Tools → Options.
    /// </summary>
    public class LearnOptionPage : DialogPage
    {
        [Category("Markdown Region Buddy")]
        [DisplayName("Enable Decorations")]
        [Description("Enable background colors for different section types")]
        public bool EnableDecorations { get; set; } = false;

        [Category("Markdown Region Buddy")]
        [DisplayName("Decoration Opacity")]
        [Description("Opacity for section background colors (0.01-0.3)")]
        public double DecorationOpacity { get; set; } = 0.05;

        /// <summary>
        /// Fired when settings are applied from the Options dialog or toggled via command.
        /// </summary>
        public static event System.EventHandler SettingsChanged;

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            SettingsChanged?.Invoke(this, System.EventArgs.Empty);
        }

        /// <summary>
        /// Raise the settings changed event from outside the dialog (e.g., toggle command).
        /// </summary>
        internal static void RaiseSettingsChanged()
        {
            SettingsChanged?.Invoke(null, System.EventArgs.Empty);
        }
    }
}
