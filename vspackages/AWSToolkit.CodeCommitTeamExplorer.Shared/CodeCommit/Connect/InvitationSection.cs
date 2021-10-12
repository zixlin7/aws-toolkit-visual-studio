using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Timers;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.Base;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controllers;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using Amazon.AWSToolkit.CommonUI;

using log4net;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Connect
{
    /// <summary>
    /// MEF Activated
    /// Manages the stock TeamExplorer panel that "Invites" users to connect to a backend.
    /// This class gives users a "Connect..." label that launches a
    /// dialog (<see cref="Controls.ConnectControl"/>) which lets users select credentials
    /// to connect to CodeCommit with.
    /// 
    /// If user connects, this panel is hidden, and <see cref="ConnectionSection"/> is shown.
    /// </summary>
    [TeamExplorerServiceInvitation(TeamExplorerInvitationSectionId, CodeCommitInvitationSectionPriority)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class InvitationSection : TeamExplorerInvitationBase
    {
        static InvitationSection()
        {
            Amazon.AWSToolkit.CodeCommit.ConnectServiceManager.ConnectService = new TeamExplorerConnectService();
        }

        private readonly ILog LOGGER = LogManager.GetLogger(typeof(InvitationSection));

        private IVsShell _vsShell = null;
        private readonly Timer _packageLoadedTimer;
        private bool _toolkitPackageLoaded = false;

        public const int CodeCommitInvitationSectionPriority = 150;
        private const int TimerIntervalMs = 2000;

        private const string _signUpUrl = "https://aws.amazon.com/";

        [ImportingConstructor]
        public InvitationSection()
        {
            LOGGER.Debug("Creating CodeCommit InvitationSection");

            _packageLoadedTimer = new Timer()
            {
                Interval = TimerIntervalMs,
                AutoReset = false,
                Enabled = false,
            };
            _packageLoadedTimer.Elapsed += CheckIfToolkitLoaded;

            // Enable the "Connect" label after we are able to initialize
            CanConnect = false;

            CanSignUp = true;
            ConnectLabel = Resources.InvitationSectionConnectLabel;
            SignUpLabel = Resources.SignUpLink;
            Name = "AWS CodeCommit";
            Provider = "Amazon, Inc.";
            Description = Resources.CodeCommitInvitationBlurbText;

            Icon = CreateDrawingBrush(ToolkitImages.CodeCommit);

            IsVisible = TeamExplorerConnection.ActiveConnection == null;
            TeamExplorerConnection.OnTeamExplorerBindingChanged += (oldConnection, newConnection) => { IsVisible = newConnection == null; };
        }

        public override void Initialize(IServiceProvider serviceProvider)
        {
            base.Initialize(serviceProvider);

            // Connecting to CodeCommit requires the Toolkit's list of Accounts, and
            // other resources (like UI Theming).
            //
            // It is possible for the Team Explorer integrations to load before
            // the AWS Toolkit Package is loaded. This can happen when the previous
            // VS session had the Team Explorer panel open, and the AWS Explorer panel closed.
            // Ideally we would force load the Toolkit Package right here, but this
            // could negatively impact the startup performance of the IDE, especially
            // if we block for the package load here.
            // Instead, we'll poll until the Package is loaded, and gate the "Connect" label.
            _vsShell = TeamExplorerServiceProvider.GetService(typeof(SVsShell)) as IVsShell;
            _packageLoadedTimer.Start();

            // Now that we've initialized, enable the Connect label.
            // We'll gate further within the Connect handler so that we can
            // message the user if the Toolkit is not ready yet.
            CanConnect = true;
        }

        public override void Connect()
        {
            LOGGER.Debug("CodeCommit Connect");

            if (!_toolkitPackageLoaded)
            {
                var message = string.Format(
                    "Unable to connect to CodeCommit until the AWS Toolkit is loaded. Try again after loading the Toolkit.{0}{0}To load the Toolkit, select AWS Explorer from the View menu.",
                    Environment.NewLine);

                VsShellUtilities.ShowMessageBox(
                    TeamExplorerServiceProvider,
                    message,
                    "Unable to connect to CodeCommit",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
                );

                return;
            }

            // If the user has no registered profiles, launch the account registration dialog 
            // and we'll use the new profile it creates. Otherwise show them their profile(s)
            // and take the selection.
            var controller = new ConnectController();
            var results = controller.Execute();
            if (results.Success)
            {
                TeamExplorerConnection.Signin(controller.SelectedAccount);
            }
        }

        public override void SignUp()
        {
            LOGGER.Debug("CodeCommit SignUp");
            try
            {
                var u = new UriBuilder(_signUpUrl)
                {
                    Scheme = "https"
                };

                Process.Start(new ProcessStartInfo(u.Uri.ToString()));
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to launch process to go to AWS sign up page", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _packageLoadedTimer.Stop();
            _packageLoadedTimer.Dispose();

            base.Dispose(disposing);
        }

        private void CheckIfToolkitLoaded(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            bool restartTimer = false;

            try
            {
                if (_vsShell == null) { return; }

                // TODO : VS2022_TeamExplorerPackageDetection : Once the TeamExplorer projects are
                // set up to leverage Shared Projects, update this code to (ifdef) conditionally
                // reference the appropriate Toolkit Package Guid (IDE-5965)
                var packageGuid = Constants.ToolkitPackageGuids.Vs20172019;
                var isLoaded = _vsShell.IsPackageLoaded(ref packageGuid, out var _);
                if (isLoaded != VSConstants.S_OK)
                {
                    restartTimer = true;
                }
                else
                {
                    _toolkitPackageLoaded = true;
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Unable to determine if the Toolkit has been loaded", e);
                restartTimer = true;
            }
            finally
            {
                if (restartTimer)
                {
                    _packageLoadedTimer.Start();
                }
            }
        }
    }
}
