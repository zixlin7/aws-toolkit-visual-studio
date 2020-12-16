using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.TeamFoundation.Controls;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.Base;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controllers;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using log4net;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Connect
{
    /// <summary>
    /// MEF Activated
    /// Manages the panel that lets users connect to CodeCommit.
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

        public const int CodeCommitInvitationSectionPriority = 150;

        private const string _signUpUrl = "https://aws.amazon.com/";

        [ImportingConstructor]
        public InvitationSection()
        {
            LOGGER.Info("Creating CodeCommit InvitationSection");
            CanConnect = true;
            CanSignUp = true;
            ConnectLabel = Resources.InvitationSectionConnectLabel;
            SignUpLabel = Resources.SignUpLink;
            Name = "AWS CodeCommit";
            Provider = "Amazon, Inc.";
            Description = Resources.CodeCommitInvitationBlurbText;

            Icon = LoadSectionIcon("Resources/CodeCommit32x32.png");

            IsVisible = TeamExplorerConnection.ActiveConnection == null;
            TeamExplorerConnection.OnTeamExplorerBindingChanged += (connection) => { IsVisible = connection == null; };
        }

        public override void Connect()
        {
            LOGGER.Info("CodeCommit Connect");
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
            LOGGER.Info("CodeCommit SignUp");
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

    }
}
