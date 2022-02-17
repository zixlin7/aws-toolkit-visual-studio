using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.Regions;

using CommonUI.Models;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class InstanceTypeSelectionDialog : DialogWindow, IInstanceTypeSelectionDialog
    {
        public const string InstanceTypesUrl = "https://aws.amazon.com/ec2/instance-types/";
        public static readonly Uri InstanceTypesUri = new Uri(InstanceTypesUrl);

        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly InstanceTypeSelectionViewModel _viewModel;

        private string _instanceTypeId;
        public string InstanceTypeId
        {
            get => _instanceTypeId;
            set => _instanceTypeId = value;
        }

        public string Filter
        {
            get => _viewModel.Filter;
            set => _viewModel.Filter = value;
        }

        public IList<string> Architectures => _viewModel.Architectures;

        public ICredentialIdentifier CredentialsId
        {
            get => _viewModel.CredentialsId;
            set => _viewModel.CredentialsId = value;
        }

        public ToolkitRegion Region
        {
            get => _viewModel.Region;
            set => _viewModel.Region = value;
        }

        public InstanceTypeSelectionDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;

            _viewModel = CreateViewModel();

            InitializeComponent();
            DataContext = _viewModel;
        }

        private InstanceTypeSelectionViewModel CreateViewModel()
        {
            var repository = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IInstanceTypeRepository)) as IInstanceTypeRepository;
            var viewModel = new InstanceTypeSelectionViewModel(repository, _joinableTaskFactory);
            viewModel.OkCommand = new RelayCommand(
                _ => _viewModel.InstanceType != null,
                _ => DialogResult = true);

            return viewModel;
        }

        public new bool Show()
        {
            _joinableTaskFactory.Run(async () =>
            {
                await LoadInstanceTypesAsync();
                await SelectAsync(_instanceTypeId);
            });

            var result = ShowModal() ?? false;

            if (result)
            {
                _instanceTypeId = _viewModel.InstanceType.Id;
            }

            return result;
        }

        private async Task LoadInstanceTypesAsync()
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            using (var progressDialog = await _toolkitContext.ToolkitHost.CreateProgressDialog())
            {
                progressDialog.Heading1 = "Loading EC2 Instance Types";
                progressDialog.CanCancel = false;
                progressDialog.Show(1);

                await _viewModel.RefreshInstanceTypesAsync().ConfigureAwait(false);

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                progressDialog.Hide();
            }
        }

        private async Task SelectAsync(string instanceTypeId)
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            _viewModel.Select(instanceTypeId);
        }

        private void ListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ListView listView) || listView.SelectedItem == null)
            {
                return;
            }

            listView.ScrollIntoView(listView.SelectedItem);
        }

        private void ListView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.OkCommand.CanExecute(null))
            {
                _viewModel.OkCommand.Execute(null);
            }
        }
    }
}
