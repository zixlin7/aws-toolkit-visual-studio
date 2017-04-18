using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Util;
using log4net;

namespace Amazon.AWSToolkit.Account.View
{
    /// <summary>
    /// Interaction logic for RegisterServiceCredentialsControl.xaml
    /// </summary>
    public partial class RegisterServiceCredentialsControl
    {
        readonly ILog LOGGER = LogManager.GetLogger(typeof(RegisterServiceCredentialsControl));

        public RegisterServiceCredentialsControl()
        {
            InitializeComponent();
        }

        public RegisterServiceCredentialsControl(RegisterServiceCredentialsController controller)
            : this()
        {
            Controller = controller;
            DataContext = Controller.Model;
            Controller.Model.PropertyChanged += ModelOnPropertyChanged;
        }

        public RegisterServiceCredentialsController Controller { get; }

        public override string Title
        {
            get { return "HTTPS Credentials for AWS CodeCommit"; }
        }

        public override bool SupportsDynamicOKEnablement
        {
            get { return true; }
        }

        public override bool Validated()
        {
            return !string.IsNullOrEmpty(Controller.Model.UserName) 
                        && !string.IsNullOrEmpty(Controller.Model.Password);
        }

        public override bool OnCommit()
        {
            return Controller.Model.PersistCredentials(Credentials);
        }

        public ServiceSpecificCredentials Credentials
        {
            get
            {
                return new ServiceSpecificCredentials
                {
                    Username = Controller.Model.UserName,
                    Password = Controller.Model.Password
                };
            }
        }
        private void OnClickCredentialsEndpoint(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Controller.OpenInBrowser(Controller.Model.IAMConsoleEndpoint);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to launch process to go to endpoint", ex);
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void OnClickImportCredentials(object sender, RoutedEventArgs e)
        {
            var csvFilename = ShowFileOpenDialog("Import AWS CodeCommit HTTPS Credentals from CSV File");
            if (csvFilename != null)
            {
                Controller.Model.ImportCredentialsFromCSV(csvFilename);
                _ctlPassword.Password = Controller.Model.Password;
            }
        }

        private static string ShowFileOpenDialog(string title)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                CheckPathExists = true,
                Title = title
            };

            var result = dlg.ShowDialog();
            if (result.GetValueOrDefault())
                return dlg.FileName;

            return null;
        }

        private void _ctlPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            Controller.Model.Password = _ctlPassword.Password;
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // throw this to our outer host, so the OK button enablement gets checked
            NotifyPropertyChanged(propertyChangedEventArgs.PropertyName);
        }


    }
}
