using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Shared;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    /// <summary>
    /// Visual Studio DialogWindow host for views that use two buttons (one accept, one decline).
    /// The dialog is set up to use Toolkit Themes (<see cref="Amazon.AWSToolkit.Themes.ToolkitThemes"/>).
    /// </summary>
    public partial class OkCancelDialogWindowHost : DialogWindow
    {
        private const int ContentMargin = 12;
        private const int ButtonVerticalSpacing = 18;
        private const int ButtonHeight = 23;

        private readonly IAWSToolkitControl _hostedControl;
        private readonly IAWSToolkitShellProvider _shellProvider;

        public OkCancelDialogWindowHost(IAWSToolkitControl hostedControl, MessageBoxButton buttons, IAWSToolkitShellProvider shellProvider)
        {
            _shellProvider = shellProvider;
            _hostedControl = hostedControl;

            if (buttons != MessageBoxButton.OKCancel && buttons != MessageBoxButton.YesNo)
            {
                throw new NotImplementedException($"{nameof(OkCancelDialogWindowHost)} does not support {buttons}");
            }

            InitializeComponent();

            Debug.Assert(hostedControl.UserControl.Height > 0 && hostedControl.UserControl.Height < double.PositiveInfinity,
                $"HostedControl {hostedControl.GetType()} must set the Height property to be used by {GetType()}.");
            Debug.Assert(hostedControl.UserControl.Width > 0 && hostedControl.UserControl.Width < double.PositiveInfinity,
                $"HostedControl {hostedControl.GetType()} must set the Width property to be used by {GetType()}.");

            Width = (int) (hostedControl.UserControl.Width + (2 * ContentMargin));
            Height = (int)(hostedControl.UserControl.Height + (2* ContentMargin) + ButtonVerticalSpacing + ButtonHeight);
            MinWidth = Width;
            MinHeight = Height;
            AddHostedControl();
            Title = _hostedControl.Title;

            HasHelpButton = _hostedControl.GetHelpHandler() != null;
            _ctlHelpButton.Visibility = HasHelpButton ? Visibility.Visible : Visibility.Collapsed;

            _ctlAcceptButton.IsEnabled = _hostedControl.SupportsDynamicOKEnablement 
                ? _hostedControl.Validated() 
                : _hostedControl.UserControl.IsEnabled;

            hostedControl.UserControl.IsEnabledChanged += HostedControlIsEnabledChanged;

            SetButtonText(buttons);
        }

        protected override void InvokeDialogHelp()
        {
            _hostedControl.GetHelpHandler()?.OnHelp();
        }

        private void SetButtonText(MessageBoxButton buttons)
        {
            if (buttons == MessageBoxButton.YesNo)
            {
                _ctlAcceptButton.Content = "Yes";
                _ctlRejectButton.Content = "No";
            }
            else
            {
                _ctlAcceptButton.Content = !string.IsNullOrWhiteSpace(_hostedControl.AcceptButtonText)
                    ? _hostedControl.AcceptButtonText
                    : "OK";
                _ctlRejectButton.Content = !string.IsNullOrWhiteSpace(_hostedControl.RejectButtonText)
                    ? _hostedControl.RejectButtonText
                    : "Cancel";
            }
        }

        private void HostedControlIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _ctlAcceptButton.IsEnabled = _hostedControl.UserControl.IsEnabled;
        }

        private void AddHostedControl()
        {
            _hostedControl.UserControl.Width = double.NaN;
            _hostedControl.UserControl.Height = double.NaN;
            Grid.SetColumn(_hostedControl.UserControl, 0);
            Grid.SetRow(_hostedControl.UserControl, 0);
            _ctlMainGrid.Children.Add(_hostedControl.UserControl);

            if (_hostedControl.SupportsDynamicOKEnablement)
            {
                _hostedControl.PropertyChanged += HostedControlOnPropertyChanged;
            }
        }

        /// <summary>
        /// A property has changed on a hosted control that supports dynamic enabling of the Ok button.
        /// </summary>
        private void HostedControlOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // Re-assess if the Ok button may be enabled.
            _ctlAcceptButton.IsEnabled = _hostedControl.Validated();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            InvokeDialogHelp();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ctlAcceptButton.Focus();
                if (!_hostedControl.Validated() || !_hostedControl.OnCommit())
                {
                    return;
                }

                // Dialog box accepted
                DialogResult = true;
            }
            catch (Exception ex)
            {
                _shellProvider.ShowError($"Error saving {_hostedControl.Title}", ex.Message);
            }
        }
    }
}
