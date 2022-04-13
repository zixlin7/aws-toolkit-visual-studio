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
using Amazon.AWSToolkit.IdentityManagement.Models;
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
        public string ServicePrincipalFilter;

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
            var roles = await _iamEntities.ListIamRolesAsync(CredentialsId, Region).ConfigureAwait(false);
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            RoleArns = new ObservableCollection<string>(roles
                .Where(IsDisplayCandidate)
                .Select(role => role.Arn)
                .OrderBy(arn => arn));
            GetRoleArnsView().Filter = RoleCollectionViewFilter;
        }

        /// <summary>
        /// Whether or not a role is eligible to be shown in the dialog
        /// </summary>
        private bool IsDisplayCandidate(IamRole role)
        {
            if (string.IsNullOrWhiteSpace(ServicePrincipalFilter)) return true;

            return
                !string.IsNullOrEmpty(role.AssumeRolePolicyDocument) &&
                role.AssumeRolePolicyDocument.Contains(ServicePrincipalFilter);
        }

        private ICollectionView GetRoleArnsView() => CollectionViewSource.GetDefaultView(RoleArns);

        private bool RoleCollectionViewFilter(object candidate)
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
