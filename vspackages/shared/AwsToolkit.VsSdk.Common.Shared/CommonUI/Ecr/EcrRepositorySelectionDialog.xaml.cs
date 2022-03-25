using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ECS.PluginServices.Ecr;
using Amazon.AWSToolkit.Regions;

using CommonUI.Models;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

namespace AwsToolkit.VsSdk.Common.CommonUI.Ecr
{
    public partial class EcrRepositorySelectionDialog : DialogWindow, IEcrRepositorySelectionDialog
    {
        public string RepositoryName
        {
            get => _repositoryName;
            set => _repositoryName = value;
        }

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

        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly RepositorySelectionViewModel _viewModel;
        private string _repositoryName;

        public EcrRepositorySelectionDialog(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;

            _viewModel = CreateViewModel();

            InitializeComponent();
            DataContext = _viewModel;
        }

        private RepositorySelectionViewModel CreateViewModel()
        {
            var repositoryFactory = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)) as IRepositoryFactory;

            if (repositoryFactory == null)
            {
                throw new Exception("Unable to load ECR Repository data source");
            }

            var viewModel = new RepositorySelectionViewModel(repositoryFactory, _joinableTaskFactory);
            viewModel.OkCommand = new RelayCommand(
                _ => _viewModel.Repository != null,
                _ => DialogResult = true);

            return viewModel;
        }

        public new bool Show()
        {
            _joinableTaskFactory.Run(async () =>
            {
                await LoadRepositoriesAsync();
                await SelectAsync(_repositoryName);
            });

            var result = ShowModal() ?? false;

            if (result)
            {
                _repositoryName = _viewModel.Repository.Name;
            }

            return result;
        }

        private async Task LoadRepositoriesAsync()
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            using (var progressDialog = await _toolkitContext.ToolkitHost.CreateProgressDialog())
            {
                progressDialog.Heading1 = "Loading ECR Repositories";
                progressDialog.CanCancel = false;
                progressDialog.Show(1);

                await _viewModel.RefreshRepositoriesAsync().ConfigureAwait(false);

                await _joinableTaskFactory.SwitchToMainThreadAsync();
                progressDialog.Hide();
            }
        }

        private async Task SelectAsync(string repositoryName)
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            _viewModel.Select(repositoryName);
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
