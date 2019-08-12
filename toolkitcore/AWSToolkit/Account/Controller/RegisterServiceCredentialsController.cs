using System;
using System.ComponentModel;
using Amazon.AWSToolkit.Account.Model;
using Amazon.AWSToolkit.Account.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Account.Controller
{
    public class RegisterServiceCredentialsController
    {
        private RegisterServiceCredentialsControl _control;

        public RegisterServiceCredentialsController(AccountViewModel account)
        {
            Model = new RegisterServiceCredentialsModel(account);
            Model.PropertyChanged += ModelOnPropertyChanged;
        }

        public RegisterServiceCredentialsModel Model { get; }

        public virtual ActionResults Execute()
        {
            this._control = new RegisterServiceCredentialsControl(this);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(this._control))
            {
                return new ActionResults().WithSuccess(true);
            }

            return new ActionResults().WithSuccess(false);
        }

        public ServiceSpecificCredentials Credentials => _control.Credentials;

        public void OpenInBrowser(string endpoint)
        {
            ToolkitFactory.Instance.ShellProvider.OpenInBrowser(endpoint, true);
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // ignore our self-inflicted updates
            if (propertyChangedEventArgs.PropertyName.Equals("IsValid", StringComparison.OrdinalIgnoreCase))
                return;

            if ((string.IsNullOrEmpty(Model.UserName) && !string.IsNullOrEmpty(Model.Password))
                    || !string.IsNullOrEmpty(Model.UserName) && string.IsNullOrEmpty(Model.Password))
                Model.IsValid = false;
            else
                Model.IsValid = true;
        }

    }
}
