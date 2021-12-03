using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.IdentityManagement;
using Amazon.AWSToolkit.Regions;

using Microsoft.VisualStudio.Threading;

namespace CommonUI.Models
{
    public class IamRoleSelectionViewModel : BaseModel
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IIamEntityRepository _iamEntities;

        public ICredentialIdentifier CredentialsId;
        public ToolkitRegion Region;

        private string _filter;

        public string Filter
        {
            get => _filter;
            set
            {
                SetProperty(ref _filter, value);
                GetRoleArnsView().Refresh();
            }
        }

        private string _roleArn;

        public string RoleArn
        {
            get => _roleArn;
            set => SetProperty(ref _roleArn, value);
        }

        private ObservableCollection<string> _roleArns;

        public ObservableCollection<string> RoleArns
        {
            get => _roleArns;
            set => SetProperty(ref _roleArns, value);
        }

        private ICommand _okCommand;

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        public IamRoleSelectionViewModel(IIamEntityRepository iamEntities, JoinableTaskFactory joinableTaskFactory)
        {
            _iamEntities = iamEntities;
            _joinableTaskFactory = joinableTaskFactory;
        }

        public async Task RefreshRolesAsync()
        {
            await TaskScheduler.Default;
            var roleArns = await _iamEntities.ListIamRoleArnsAsync(CredentialsId, Region).ConfigureAwait(false);
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            RoleArns = new ObservableCollection<string>(roleArns.OrderBy(s => s));
            GetRoleArnsView().Filter = FilterRole;
        }

        private ICollectionView GetRoleArnsView() => CollectionViewSource.GetDefaultView(RoleArns);

        private bool FilterRole(object candidate)
        {
            return IsObjectFiltered(candidate, Filter);
        }

        public static bool IsObjectFiltered(object candidate, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            if (!(candidate is string roleArn))
            {
                return false;
            }

            return roleArn.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
    }
}
