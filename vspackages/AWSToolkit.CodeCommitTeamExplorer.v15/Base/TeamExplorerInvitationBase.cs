using System;
using Microsoft.TeamFoundation.Controls;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.Base
{
    public class TeamExplorerInvitationBase : TeamExplorerBase, ITeamExplorerServiceInvitation
    {
        public const string TeamExplorerInvitationSectionId = "C2443FCC-6D62-4D31-B08A-C4DE70109C7F";

        private bool _canConnect;
        private bool _canSignUp;
        private string _connectLabel;
        private string _description;
        private object _icon;
        private bool _isVisible;
        private string _name;
        private string _provider;
        private string _signUpLabel;

        #region ITeamExplorerServiceInvitation

        /// <summary>
        /// Triggers the login flow
        /// </summary>
        public virtual void Connect() { }

        /// <summary>
        /// Starts the sign up process online
        /// </summary>
        public virtual void SignUp() { }

        public bool CanConnect
        {
            get => _canConnect;
            set { _canConnect = value; this.RaisePropertyChanged(); }
        }

        public bool CanSignUp
        {
            get => _canSignUp;
            set { _canSignUp = value; this.RaisePropertyChanged(); }
        }

        public string ConnectLabel
        {
            get => _connectLabel;
            set { _connectLabel = value; this.RaisePropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; this.RaisePropertyChanged(); }
        }

        public object Icon
        {
            get => _icon;
            set { _icon = value; this.RaisePropertyChanged(); }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; this.RaisePropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; this.RaisePropertyChanged(); }
        }

        public string Provider
        {
            get => _provider;
            set { _provider = value; this.RaisePropertyChanged(); }
        }

        public string SignUpLabel
        {
            get => _signUpLabel;
            set { _signUpLabel = value; this.RaisePropertyChanged(); }
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            TeamExplorerServiceProvider = serviceProvider;
        }

        #endregion
    }
}
