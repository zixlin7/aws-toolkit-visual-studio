using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.Runtime;
using Amazon.Runtime.Credentials.Internal;

using AwsToolkit.VsSdk.Common.CommonUI.Models;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    /// <summary>
    /// Interaction logic for SsoLoginDialog.xaml
    /// </summary>
    public partial class SsoLoginDialog : ISsoLoginDialog
    {
        private SsoLoginViewModel _viewModel => DataContext as SsoLoginViewModel;

        public SsoLoginDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public ImmutableCredentials Credentials
        {
            get => _viewModel.Credentials;
            set => _viewModel.Credentials = value;
        }

        public SsoToken SsoToken
        {
            get => _viewModel.SsoToken;
            set => _viewModel.SsoToken = value;
        }

        public TaskResult DoLoginFlow()
        {
            ShowModal();
            _viewModel.RecordLoginMetric();
            return _viewModel.LoginResult;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.BeginLoginFlow();
            Loaded -= OnLoaded;
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
        }
    }
}
