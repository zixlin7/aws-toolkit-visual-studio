using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Regions;

using Microsoft.VisualStudio.Threading;

namespace CommonUI.Models
{
    public class VpcSelectionViewModel : BaseModel
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IVpcRepository _vpcRepository;

        public ICredentialIdentifier CredentialsId;
        public ToolkitRegion Region;

        private string _filter;

        public string Filter
        {
            get => _filter;
            set
            {
                SetProperty(ref _filter, value);
                GetVpcsView().Refresh();
            }
        }

        private VpcEntity _vpc;

        public VpcEntity Vpc
        {
            get => _vpc;
            set => SetProperty(ref _vpc, value);
        }

        private ObservableCollection<VpcEntity> _vpcs;

        public ObservableCollection<VpcEntity> Vpcs
        {
            get => _vpcs;
            set => SetProperty(ref _vpcs, value);
        }

        private ICommand _okCommand;

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        public VpcSelectionViewModel(IVpcRepository vpcRepository, JoinableTaskFactory joinableTaskFactory)
        {
            _vpcRepository = vpcRepository;
            _joinableTaskFactory = joinableTaskFactory;
        }

        public async Task RefreshVpcsAsync()
        {
            await TaskScheduler.Default;
            var vpcs = await _vpcRepository.ListVpcsAsync(CredentialsId, Region).ConfigureAwait(false);
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            Vpcs = new ObservableCollection<VpcEntity>(vpcs.OrderBy(v => v.Id));
            GetVpcsView().Filter = FilterVpc;
        }

        private ICollectionView GetVpcsView() => CollectionViewSource.GetDefaultView(Vpcs);

        private bool FilterVpc(object candidate)
        {
            return IsObjectFiltered(candidate, Filter);
        }

        public static bool IsObjectFiltered(object candidate, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            if (!(candidate is VpcEntity vpc))
            {
                return false;
            }

            return vpc.Id.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0
                || vpc.Description.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public void Select(string vpcId)
        {
            Vpc = Vpcs.FirstOrDefault(v => v.Id == vpcId);
        }
    }
}
