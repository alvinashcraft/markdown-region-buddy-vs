using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace vs_md_extension_buddy
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(vs_md_extension_buddyPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(LearnOptionPage), "Markdown Region Buddy", "General", 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class vs_md_extension_buddyPackage : AsyncPackage
    {
        public const string PackageGuidString = "8b2e1ea7-72df-455a-8d97-a2a7e28bbb00";

        public static vs_md_extension_buddyPackage Instance { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Instance = this;
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await LearnFoldingCommands.InitializeAsync(this);
        }
    }
}
