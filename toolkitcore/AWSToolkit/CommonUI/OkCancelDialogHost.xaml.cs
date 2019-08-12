using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Interaction logic for DialogHost.xaml
    /// </summary>
    public partial class OkCancelDialogHost : Window
    {
        IAWSToolkitControl _hostedControl;

        public OkCancelDialogHost(IAWSToolkitControl hostedControl)
            : this(hostedControl, MessageBoxButton.OKCancel)
        {
            SetResourceReference(Control.BackgroundProperty, "awsDialogBackgroundBrushKey");
            SetResourceReference(Control.ForegroundProperty, "awsDialogTextBrushKey");
        }

        public OkCancelDialogHost(IAWSToolkitControl hostedControl, MessageBoxButton buttons)
        {
            if (buttons != MessageBoxButton.OKCancel && buttons != MessageBoxButton.YesNo)
                throw new NotImplementedException("OkCancelDialogHost for " + buttons.ToString() + " is not implemented. Only OKCancel or YesNo modes are supported.");

            this._hostedControl = hostedControl;
            InitializeComponent();

            this.Width = (int)(hostedControl.UserControl.Width + 50);
            this.Height = (int)(hostedControl.UserControl.Height + 100);
            addHostedControl();
            this.Title = this._hostedControl.Title;

            _ctlAcceptButton.IsEnabled = _hostedControl.SupportsDynamicOKEnablement 
                ? _hostedControl.Validated() 
                : _hostedControl.UserControl.IsEnabled;

            hostedControl.UserControl.IsEnabledChanged += hostedControlIsEnabledChanged;

            if (buttons == MessageBoxButton.YesNo)
            {
                _ctlAcceptButton.Content = "Yes";
                _ctlRejectButton.Content = "No";
            }
        }

        /// <summary>
        /// Responds to the property change notification fired from the hosted control,
        /// if it declared support for dynamic accept button enablement, to toggle the
        /// enablement of the accept button based on the control's current validity.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="propertyChangedEventArgs"></param>
        private void HostedControlOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            _ctlAcceptButton.IsEnabled = _hostedControl.Validated();
        }

        void hostedControlIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._ctlAcceptButton.IsEnabled = this._hostedControl.UserControl.IsEnabled;
        }

        public IAWSToolkitControl HostedControl => this._hostedControl;

        private void addHostedControl()
        {
            this._hostedControl.UserControl.Width = double.NaN;
            this._hostedControl.UserControl.Height = double.NaN;
            Grid.SetColumn(this._hostedControl.UserControl, 0);
            Grid.SetColumnSpan(this._hostedControl.UserControl, 2);
            Grid.SetRow(this._hostedControl.UserControl, 0);
            Grid.SetRowSpan(this._hostedControl.UserControl, 3);
            this._ctlMainGrid.Children.Add(this._hostedControl.UserControl);

            if (_hostedControl.SupportsDynamicOKEnablement)
                _hostedControl.PropertyChanged += HostedControlOnPropertyChanged;
        }

        void acceptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this._ctlAcceptButton.Focus();
                if (!this._hostedControl.Validated() || !this._hostedControl.OnCommit())
                    return;

                // Dialog box accepted
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Error saving {0}", this._hostedControl.Title), ex.Message);
            }
        }

        public bool IsOkEnabled
        {
            get => this._ctlAcceptButton.IsEnabled;
            set => this._ctlAcceptButton.IsEnabled = value;
        }

        public void Close(bool dialogResult)
        {
            this.DialogResult = dialogResult;
            this.Close();
        }
    }
}
