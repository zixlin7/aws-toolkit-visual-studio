using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Regions.Manifest;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Controls
{
    /// <summary>
    /// A convience class that can be added to view models to support functionality of the RegionSelector control.
    /// </summary>
    /// <remarks>
    /// The RegionSelectorMixin can be added to view models to support the necessary properties
    /// of the RegionSelector control.  It handles automatic data loading and updating region lists
    /// on selected partitions.  This class is merely a convenience and view models may support this
    /// functionality directly.
    /// </remarks>
    public class RegionSelectorMixin : BaseModel
    {
        private readonly ToolkitContext _toolkitContext;

        private readonly Action<ToolkitRegion> _selectedRegionChangedHandler;

        private IEnumerable<Partition> _partitions;

        public IEnumerable<Partition> Partitions
        {
            get => _partitions;
            set => SetProperty(ref _partitions, value);
        }

        private Partition _selectedPartition;

        public Partition SelectedPartition
        {
            get => _selectedPartition;
            set => SetProperty(ref _selectedPartition, value);
        }

        private IEnumerable<ToolkitRegion> _regions;

        public IEnumerable<ToolkitRegion> Regions
        {
            get => _regions;
            set => SetProperty(ref _regions, value);
        }

        private ToolkitRegion _selectedRegion;

        public ToolkitRegion SelectedRegion
        {
            get => _selectedRegion;
            set => SetProperty(ref _selectedRegion, DetermineBestRegion(value));
        }

        private ToolkitRegion DetermineBestRegion(ToolkitRegion desiredRegion)
        {
            return
                Regions.FirstOrDefault(r => r.Id == desiredRegion?.Id) ??
                Regions.FirstOrDefault(r => r.Id == ToolkitRegion.DefaultRegionId) ??
                Regions.FirstOrDefault();
        }

        /// <summary>
        /// Creates a new instance of RegionSelectorMixin
        /// </summary>
        /// <param name="toolkitContext">A ToolkitContext for which RegionProvider is used.</param>
        /// <param name="selectedRegionChangedHandler">A callback to be called whenever SelectedRegion is updated.</param>
        /// <remarks>
        /// While this class implements INotifyPropertyChanged, the selectedRegionChangedHandler is provided
        /// as a convenience to not require adding an event handler to PropertyChanged.  It is also called on
        /// initialization so that the consuming view model always has the region set.
        /// </remarks>
        public RegionSelectorMixin(ToolkitContext toolkitContext, Action<ToolkitRegion> selectedRegionChangedHandler = null)
        {
            _toolkitContext = toolkitContext;
            _selectedRegionChangedHandler = selectedRegionChangedHandler;

            PropertyChanged += RegionSelectorViewModel_PropertyChanged;

            Partitions = _toolkitContext.RegionProvider.GetPartitions();
            SelectedPartition = Partitions.FirstOrDefault(p => p.Id == PartitionIds.DefaultPartitionId);

            // Ensure listener gets the intial value of SelectedRegion
            _selectedRegionChangedHandler?.Invoke(SelectedRegion);
        }

        private void RegionSelectorViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedPartition):
                    if (SelectedPartition != null)
                    {
                        Regions = _toolkitContext.RegionProvider.GetRegions(SelectedPartition.Id)
                            .Where(r => !_toolkitContext.RegionProvider.IsRegionLocal(r.Id));
                    }
                    break;
                case nameof(Regions):
                    // Try to reset the same region, but SelectedRegion will DetermineBestRegion
                    SelectedRegion = SelectedRegion;
                    break;
                case nameof(SelectedRegion):
                    _selectedRegionChangedHandler?.Invoke(SelectedRegion);
                    break;
            }
        }
    }
}
