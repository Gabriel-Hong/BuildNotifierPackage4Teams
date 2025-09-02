using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;
using BuildNotifierPackagesForTeams.Models;

namespace BuildNotifierPackagesForTeams.Services
{
    public class BuildEventListener : IVsUpdateSolutionEvents2, IDisposable
    {
        private IVsSolutionBuildManager2 buildManager;
        private uint cookie;
        private readonly TeamsNotificationService teamsService;
        private DateTime buildStartTime;

        public BuildEventListener(TeamsNotificationService teamsService)
        {
            this.teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        public async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            buildManager = await package.GetServiceAsync(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
            if (buildManager != null)
            {
                buildManager.AdviseUpdateSolutionEvents(this, out cookie);
            }
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            buildStartTime = DateTime.Now;
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                var buildTime = DateTime.Now - buildStartTime;
                var result = new BuildResult
                {
                    Success = fSucceeded != 0,
                    BuildTime = buildTime,
                    Timestamp = DateTime.Now,
                    ProjectName = await GetSolutionNameAsync()
                };

                await teamsService.SendBuildNotificationAsync(result);
            });

            return VSConstants.S_OK;
        }

        private async Task<string> GetSolutionNameAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var solution = await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            if (solution != null)
            {
                solution.GetSolutionInfo(out string solutionDir, out string solutionFile, out string userOptsFile);
                return System.IO.Path.GetFileNameWithoutExtension(solutionFile) ?? "Unknown Solution";
            }
            return "Unknown Solution";
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;
        public int UpdateSolution_Cancel() => VSConstants.S_OK;
        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;

        // Additional required methods for IVsUpdateSolutionEvents2
        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            if (buildManager != null && cookie != 0)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    buildManager.UnadviseUpdateSolutionEvents(cookie);
                });
            }
        }
    }
}