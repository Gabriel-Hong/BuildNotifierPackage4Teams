using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using BuildNotifierPackagesForTeams.Services;
using BuildNotifierPackagesForTeams.Commands;

namespace BuildNotifierPackagesForTeams
{
  /// <summary>
  /// This is the class that implements the package exposed by this assembly.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The minimum requirement for a class to be considered a valid package for Visual Studio
  /// is to implement the IVsPackage interface and register itself with the shell.
  /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
  /// to do it: it derives from the Package class that provides the implementation of the
  /// IVsPackage interface and uses the registration attributes defined in the framework to
  /// register itself and its components with the shell. These attributes tell the pkgdef creation
  /// utility what data to put into .pkgdef file.
  /// </para>
  /// <para>
  /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
  /// </para>
  /// </remarks>
  [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
  [Guid(BuildNotifierPackagesForTeamsPackage.PackageGuidString)]
  [ProvideMenuResource("Menus.ctmenu", 1)]
  [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
  public sealed class BuildNotifierPackagesForTeamsPackage : AsyncPackage
  {
    /// <summary>
    /// BuildNotifierPackagesForTeamsPackage GUID string.
    /// </summary>
    public const string PackageGuidString = "60f2a757-3a27-47ff-91c7-08137757d977";

    private BuildEventListener buildEventListener;
    private TeamsNotificationService teamsService;

    #region Package Members

    /// <summary>
    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
    /// <param name="progress">A provider for progress updates.</param>
    /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
      try
      {
        // When initialized asynchronously, the current thread may be a background thread at this point.
        // Do any initialization that requires the UI thread after switching to the UI thread.
        await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // 약간의 지연을 추가하여 Visual Studio가 완전히 로드되기를 기다림
        await Task.Delay(1000, cancellationToken);

        // Initialize services
        System.Diagnostics.Debug.WriteLine("Initializing TeamsNotificationService");
        teamsService = new TeamsNotificationService();
        
        System.Diagnostics.Debug.WriteLine("Initializing BuildEventListener");
        buildEventListener = new BuildEventListener(teamsService);

        // Initialize build event listener
        await buildEventListener.InitializeAsync(this);

        // Initialize commands
        System.Diagnostics.Debug.WriteLine("Initializing ConfigureTeamsCommand");
        await ConfigureTeamsCommand.InitializeAsync(this, teamsService);
        
        System.Diagnostics.Debug.WriteLine("BuildNotifierPackagesForTeams initialized successfully.");
      }
      catch (OperationCanceledException)
      {
        // 정상적인 취소 - 로그하지 않음
        throw;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Failed to initialize BuildNotifierPackagesForTeams: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        
        // 에러가 발생해도 Visual Studio가 크래시되지 않도록 예외를 삼킴
        // 하지만 중요한 초기화 실패는 로그에 기록
      }
    }

    protected override void Dispose(bool disposing)
    {
      try
      {
        if (disposing)
        {
          buildEventListener?.Dispose();
          teamsService?.Dispose();
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error during dispose: {ex.Message}");
      }
      finally
      {
        base.Dispose(disposing);
      }
    }

    #endregion
  }
}
