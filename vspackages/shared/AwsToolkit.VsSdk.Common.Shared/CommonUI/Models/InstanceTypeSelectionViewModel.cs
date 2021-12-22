using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.Regions;

using Microsoft.VisualStudio.Threading;

namespace CommonUI.Models
{
    public class InstanceTypeSelectionViewModel : BaseModel
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IInstanceTypeRepository _instanceTypeRepository;

        public ICredentialIdentifier CredentialsId;
        public ToolkitRegion Region;

        private string _filter;

        public string Filter
        {
            get => _filter;
            set
            {
                SetProperty(ref _filter, value);
                GetInstanceTypesView()?.Refresh();
            }
        }

        public IList<string> Architectures { get; } = new List<string>();

        private InstanceTypeModel _instanceType;

        public InstanceTypeModel InstanceType
        {
            get => _instanceType;
            set => SetProperty(ref _instanceType, value);
        }

        private ObservableCollection<InstanceTypeModel> _instanceTypes;

        public ObservableCollection<InstanceTypeModel> InstanceTypes
        {
            get => _instanceTypes;
            set => SetProperty(ref _instanceTypes, value);
        }

        private ICommand _okCommand;

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        public InstanceTypeSelectionViewModel(IInstanceTypeRepository instanceTypeRepository, JoinableTaskFactory joinableTaskFactory)
        {
            _instanceTypeRepository = instanceTypeRepository;
            _joinableTaskFactory = joinableTaskFactory;
        }

        public async Task RefreshInstanceTypesAsync()
        {
            await TaskScheduler.Default;
            var selectedId = InstanceType?.Id;
            var instanceTypes = (await _instanceTypeRepository
                .ListInstanceTypesAsync(CredentialsId, Region)
                .ConfigureAwait(false))
                .Where(CanDisplay);
            await _joinableTaskFactory.SwitchToMainThreadAsync();
            InstanceTypes = new ObservableCollection<InstanceTypeModel>(instanceTypes.OrderBy(instanceType => instanceType.Id));
            InstanceType = InstanceTypes.FirstOrDefault(i => i.Id == selectedId);
            GetInstanceTypesView().Filter = FilterInstanceType;
        }

        private bool CanDisplay(InstanceTypeModel instanceType)
        {
            if (!Architectures.Any())
            {
                return true;
            }

            return Architectures.Intersect(instanceType.Architectures).Any();
        }

        private ICollectionView GetInstanceTypesView() => CollectionViewSource.GetDefaultView(InstanceTypes);

        private bool FilterInstanceType(object candidate)
        {
            return IsObjectFiltered(candidate, Filter);
        }

        public static bool IsObjectFiltered(object candidate, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            if (!(candidate is InstanceTypeModel instanceType))
            {
                return false;
            }

            var filters = filter.Split(new [] { " " }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            var candidates = GetFilterByText(instanceType).ToList();
            return filters.All(f => MatchesOneOrMore(candidates, f));
        }

        private static IEnumerable<string> GetFilterByText(InstanceTypeModel instanceType)
        {
            var texts = new List<string>();
            texts.Add(instanceType.Id);

            instanceType.Architectures?.ForEach(texts.Add);
            return texts;
        }

        private static bool MatchesOneOrMore(IEnumerable<string> texts, string filter)
        {
            return texts.Any(text => Contains(text, filter));
        }

        private static bool Contains(string text, string filter)
        {
            return text.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public void Select(string instanceTypeId)
        {
            InstanceType = InstanceTypes.FirstOrDefault(instanceType => instanceType.Id == instanceTypeId);
        }
    }
}
