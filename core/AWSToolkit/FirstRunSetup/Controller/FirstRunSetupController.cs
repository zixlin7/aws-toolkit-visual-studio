using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.FirstRunSetup.View;
using Amazon.AWSToolkit.FirstRunSetup.Model;
using Amazon.AWSToolkit.Navigator;
using log4net;
using Microsoft.Win32;

namespace Amazon.AWSToolkit.FirstRunSetup.Controller
{
    public class FirstRunSetupController
    {
        const string ShowFirstRunDialogKey = "ShowFirstRunDialog";

        readonly FirstRunSetupModel _model;
        FirstRunSetupControl _control;

        internal static ILog LOGGER = LogManager.GetLogger(typeof(FirstRunSetupController));

        protected ActionResults _results;

        public FirstRunSetupController()
        {
            this._model = new FirstRunSetupModel();
        }

        /// <summary>
        /// Checks the registry to see if the welcome/first-run dialog has already
        /// been seen by the user and cancelled (if run to completion then we have
        /// a registered account and this won't be called).
        /// </summary>
        internal static bool ShowOnStartup
        {
            get
            {
                var accounts = ToolkitFactory.Instance.RootViewModel.RegisteredAccounts;
                var showOnStartup = !(accounts != null && accounts.Count > 0);
                if (showOnStartup)
                {
                    try
                    {
                        using (var k = Registry.CurrentUser.CreateSubKey(Constants.AWSToolkitRegistryKey))
                        {
                            showOnStartup =
                                bool.Parse((string)k.GetValue(ShowFirstRunDialogKey, showOnStartup.ToString()));
                        }
                    }
                    catch (Exception e)
                    {
                        LOGGER.ErrorFormat("Caught exception during ShowOnStartup registry update, {0} stack {1}", e.Message, e.StackTrace);
                    }
                }

                return showOnStartup;
            }

            private set
            {
                try
                {
                    using (var k = Registry.CurrentUser.CreateSubKey(Constants.AWSToolkitRegistryKey))
                    {
                        k.SetValue(ShowFirstRunDialogKey, value.ToString(), RegistryValueKind.String);
                    }
                }
                catch { }
            }
        }

        public FirstRunSetupModel Model
        {
            get { return this._model; }
        }

        public ActionResults Execute()
        {
            try
            {
                this._control = new FirstRunSetupControl(this);
                ToolkitFactory.Instance.ShellProvider.ShowModalFrameless(this._control);
                if (this._model.OpenExplorerOnExit)
                    ToolkitFactory.Instance.ShellProvider.OpenShellWindow(ShellWindows.Explorer);
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("First run controller caught exception loading dialog, details: {0}, stack {1}", e.Message, e.StackTrace);
            }
            finally
            {
                // this is a single opportunity dialog
                ShowOnStartup = false;
            }

            return new ActionResults().WithSuccess(true);
        }

        public void Persist()
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);

            Guid accountKey = Guid.NewGuid();
            var os = settings.NewObjectSettings(accountKey.ToString());
            os[ToolkitSettingsConstants.AccessKeyField] = this.Model.AccessKey.Trim();
            os[ToolkitSettingsConstants.DisplayNameField] = this.Model.DisplayName.Trim();
            os[ToolkitSettingsConstants.SecretKeyField] = this.Model.SecretKey.Trim();
            os[ToolkitSettingsConstants.AccountNumberField] = this.Model.AccountNumber == null ? null : this.Model.AccountNumber.Trim();
            os[ToolkitSettingsConstants.Restrictions] = null;

            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.RegisteredProfiles, settings);

            RegionEndPointsManager.Instance.SetDefaultRegionEndPoints(this.Model.SelectedRegion);
            ToolkitFactory.Instance.Navigator.UpdateAccountSelection(accountKey, true);
        }
    }
}
