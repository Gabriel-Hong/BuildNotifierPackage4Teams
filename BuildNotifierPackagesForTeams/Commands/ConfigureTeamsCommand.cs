using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using BuildNotifierPackagesForTeams.Services;
using BuildNotifierPackagesForTeams.UI;

namespace BuildNotifierPackagesForTeams.Commands
{
    internal sealed class ConfigureTeamsCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("8c2b3c3b-69d4-462e-ac34-3b27e4b2a5c9");

        private readonly AsyncPackage package;
        private readonly TeamsNotificationService teamsService;

        private ConfigureTeamsCommand(AsyncPackage package, TeamsNotificationService teamsService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        public static ConfigureTeamsCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package, TeamsNotificationService teamsService)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            
            try
            {
                System.Diagnostics.Debug.WriteLine("Getting IMenuCommandService from package");
                
                // 패키지에서 직접 서비스 가져오기
                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
                
                if (commandService != null)
                {
                    System.Diagnostics.Debug.WriteLine("IMenuCommandService obtained successfully");
                    
                    var instance = new ConfigureTeamsCommand(package, teamsService);
                    var menuCommandID = new CommandID(CommandSet, CommandId);
                    var menuItem = new MenuCommand(instance.Execute, menuCommandID);
                    commandService.AddCommand(menuItem);
                    
                    Instance = instance;
                    System.Diagnostics.Debug.WriteLine($"Command added successfully. GUID: {CommandSet}, ID: {CommandId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: IMenuCommandService is still null from package");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ConfigureTeamsCommand.InitializeAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ConfigureTeamsCommand.Execute called");
                ThreadHelper.ThrowIfNotOnUIThread();

                System.Diagnostics.Debug.WriteLine("Creating ConfigurationDialog");
                var dialog = new ConfigurationDialog(teamsService);
                
                System.Diagnostics.Debug.WriteLine("Showing dialog");
                dialog.ShowDialog();
                
                System.Diagnostics.Debug.WriteLine("Dialog closed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ConfigureTeamsCommand.Execute: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}