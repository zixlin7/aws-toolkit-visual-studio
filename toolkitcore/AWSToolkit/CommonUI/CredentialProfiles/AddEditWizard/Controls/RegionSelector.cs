using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Regions.Manifest;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Controls
{
    /// <summary>
    /// Provides a basic selector for partitions and regions.
    /// </summary>
    /// <remarks>
    /// While not required for this control, the RegionSelectorMixin is a class that can be added
    /// to view models to support the necessary properties of this control and handle automatic
    /// data loading and updating region lists on selected partitions.  This is merely a convenience
    /// and view models may support this functionality directly.
    /// </remarks>
    public class RegionSelector : Control
    {
        static RegionSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RegionSelector),
                new FrameworkPropertyMetadata(typeof(RegionSelector)));
        }

        public static readonly DependencyProperty PartitionsProperty = DependencyProperty.Register(
            "Partitions",
            typeof(IEnumerable<Partition>),
            typeof(RegionSelector),
            new FrameworkPropertyMetadata());

        public IEnumerable<Partition> Partitions
        {
            get => (IEnumerable<Partition>) GetValue(PartitionsProperty);
            set => SetValue(PartitionsProperty, value);
        }

        public static readonly DependencyProperty SelectedPartitionProperty = DependencyProperty.Register(
            "SelectedPartition",
            typeof(Partition),
            typeof(RegionSelector),
            new FrameworkPropertyMetadata()
            {
                BindsTwoWayByDefault = true
            });

        public Partition SelectedPartition
        {
            get => (Partition) GetValue(SelectedPartitionProperty);
            set => SetValue(SelectedPartitionProperty, value);
        }

        public static readonly DependencyProperty RegionsProperty = DependencyProperty.Register(
            "Regions",
            typeof(IEnumerable<ToolkitRegion>),
            typeof(RegionSelector),
            new FrameworkPropertyMetadata());

        public IEnumerable<ToolkitRegion> Regions
        {
            get => (IEnumerable<ToolkitRegion>) GetValue(RegionsProperty);
            set => SetValue(RegionsProperty, value);
        }

        public static readonly DependencyProperty SelectedRegionProperty = DependencyProperty.Register(
            "SelectedRegion",
            typeof(ToolkitRegion),
            typeof(RegionSelector),
            new FrameworkPropertyMetadata()
            {
                BindsTwoWayByDefault = true
            });

        public ToolkitRegion SelectedRegion
        {
            get => (ToolkitRegion) GetValue(SelectedRegionProperty);
            set => SetValue(SelectedRegionProperty, value);
        }
    }
}
