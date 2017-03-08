using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Model;
using Amazon.AWSToolkit.VisualStudio.FirstRun.View;
using log4net;

namespace Amazon.AWSToolkit.VisualStudio.FirstRun.Controller
{
    /// <summary>
    /// Controller class for the 'first run' experience when the toolkit
    /// determines no credentials have been set up.
    /// </summary>
    public class FirstRunController
    {
        internal static ILog LOGGER = LogManager.GetLogger(typeof(FirstRunController));

        protected ActionResults _results;
        private FirstRunControl _control;

        public FirstRunController(AWSToolkitPackage hostPackage)
        {
            HostPackage = hostPackage;
            Model = new FirstRunModel();  
            Model.PropertyChanged += ModelOnPropertyChanged;  
        }

        public FirstRunModel Model { get; }

        public AWSToolkitPackage HostPackage { get; private set; }

        /// <summary>
        /// Checks to see if any registered credential profiles are available. If
        /// not, the first run page can be displayed in the IDE.
        /// </summary>
        internal static bool ShouldShowFirstRunSetupPage
        {
            get
            {
#if DEBUG
                var accounts = ToolkitFactory.Instance.RootViewModel.RegisteredAccounts;
                return !accounts.Any();
#else
                return false;
#endif
            }
        }

        public ActionResults Execute()
        {
            try
            {
                this._control = new FirstRunControl(this);
                ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
            }
            catch (Exception e)
            {
                LOGGER.Error("First run controller caught exception loading control", e);
            }

            return new ActionResults().WithSuccess(true);
        }

        public void OpenInBrowser(string endpoint)
        {
            HostPackage.ToolkitShellProviderService.OpenInBrowser(endpoint, true);
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // ignore our self-inflicted updates
            if (propertyChangedEventArgs.PropertyName.Equals("IsValid", StringComparison.OrdinalIgnoreCase))
                return;

            Model.IsValid = !string.IsNullOrEmpty(Model.AccessKey) && !string.IsNullOrEmpty(Model.SecretKey);
        }
    }
}