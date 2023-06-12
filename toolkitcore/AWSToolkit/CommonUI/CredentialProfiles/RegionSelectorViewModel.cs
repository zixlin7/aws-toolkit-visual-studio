using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Regions.Manifest;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles
{
    public class RegionSelectorViewModel : BaseModel, IDisposable
    {
        private readonly ToolkitContext _toolkitContext;

        private bool _disposed;

        #region Partition
        private readonly ObservableCollection<Partition> _partitions = new ObservableCollection<Partition>();

        public IEnumerable<Partition> Partitions => _partitions;

        private string _selectedPartitionId;

        public string SelectedPartitionId
        {
            get => _selectedPartitionId;
            set => SetProperty(ref _selectedPartitionId, value);
        }
        #endregion

        #region Region
        private readonly ObservableCollection<ToolkitRegion> _regions = new ObservableCollection<ToolkitRegion>();

        public IEnumerable<ToolkitRegion> Regions => _regions;

        private readonly Func<string> _regionIdGetter;

        private readonly Action<string> _regionIdSetter;

        public string SelectedRegionId
        {
            get => _regionIdGetter();
            set
            {
                if (!string.Equals(SelectedRegionId, value))
                {
                    _regionIdSetter(value);
                    SelectedPartitionId = _toolkitContext.RegionProvider.GetPartitionId(value);
                    NotifyPropertyChanged(nameof(SelectedRegionId));
                }
            }
        }
        #endregion

        public RegionSelectorViewModel(ToolkitContext toolkitContext, Func<string> regionIdGetter, Action<string> regionIdSetter)
        {
            _toolkitContext = toolkitContext;
            _regionIdGetter = regionIdGetter;
            _regionIdSetter = regionIdSetter;

            PropertyChanged += RegionSelectorViewModel_PropertyChanged;

            LoadPartitions();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                PropertyChanged -= RegionSelectorViewModel_PropertyChanged;
            }
        }

        private void RegionSelectorViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedPartitionId):
                    LoadRegions();
                    break;
            }
        }

        private void LoadPartitions()
        {
            _partitions.Clear();
            _partitions.AddAll(_toolkitContext.RegionProvider.GetPartitions());

            SelectedPartitionId = SelectedRegionId != null ?
                _toolkitContext.RegionProvider.GetPartitionId(SelectedRegionId) :
                PartitionIds.DefaultPartitionId;
        }

        private void LoadRegions()
        {
            _regions.Clear();

            if (SelectedPartitionId != null)
            {
                _regions.AddAll(_toolkitContext.RegionProvider.GetRegions(SelectedPartitionId)
                    .Where(r => !_toolkitContext.RegionProvider.IsRegionLocal(r.Id)));

                if (!ContainsRegion(SelectedRegionId))
                {
                    SelectedRegionId = ContainsRegion(ToolkitRegion.DefaultRegionId) ?
                        ToolkitRegion.DefaultRegionId :
                        _regions.FirstOrDefault()?.Id;
                }
            }
        }

        private bool ContainsRegion(string regionId)
        {
            return _regions.Any(r => r.Id == regionId);
        }
    }
}
