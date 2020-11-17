﻿using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.CodeArtifact.Utils;
using Amazon.AWSToolkit.CodeArtifact.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Shared;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Amazon.AWSToolkit.CodeArtifact.Controller
{
    public class SelectProfileController : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(SelectProfileController));
        private IList<AccountViewModel> _registeredAccounts;
        private IAWSToolkitShellProvider _shellProvider;

        public SelectProfileController() : this(ToolkitFactory.Instance.RootViewModel.RegisteredAccounts, ToolkitFactory.Instance.ShellProvider) { }

        public SelectProfileController(IList<AccountViewModel> registeredAccounts, IAWSToolkitShellProvider shellProvider)
        {
            this._registeredAccounts = registeredAccounts;
            this._shellProvider = shellProvider;
        }

        public override ActionResults Execute(IViewModel model)
        {
            return _registeredAccounts.Any() ? SelectFromExistingProfiles() : NoProfile();
        }

        private ActionResults SelectFromExistingProfiles()
        {
            var control = new SelectProfileControl(this);
            if (_shellProvider.ShowModal(control))
            {
                return Persist(control.SelectedAccount);
            }

            return new ActionResults().WithSuccess(false);
        }

        private ActionResults NoProfile()
        {
            _shellProvider.ShowError("No AWS Credentials were found. Please setup one and try again");

            return new ActionResults().WithSuccess(false);
        }

        public ActionResults Persist(AccountViewModel selectedAccount)
        {
            try
            {
                if(selectedAccount == null)
                {
                    return new ActionResults().WithSuccess(false);
                }
                string profileName = selectedAccount.Profile.Name;
                ConfigureCommand(profileName);            

                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(profileName)
                    .WithShouldRefresh(true);

            }
            catch (Exception e)
            {
                LOGGER.Error("Error updating configuration: ", e);
                _shellProvider.ShowError("Error updating configuration: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }

        private void ConfigureCommand(string profile)
        {
            var configuration = Configuration.LoadInstalledConfiguration();
            configuration.DefaultProfile = profile;
            configuration.SaveInstallPath();

        }

    }
}
