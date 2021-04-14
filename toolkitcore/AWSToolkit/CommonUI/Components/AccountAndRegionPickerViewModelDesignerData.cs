using System.Collections.ObjectModel;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// The control <see cref="AccountAndRegionPicker"/> relies on the singleton
    /// <see cref="ToolkitFactory.Instance"/>, which causes errors when looking
    /// at UIs in the XAML designer. This is a class to provide design-time data
    /// so that the XAML designer renders properly.
    /// </summary>
    internal class AccountAndRegionPickerViewModelDesignerData : AccountAndRegionPickerViewModel
    {
        public new ToolkitRegion Region;
        public new ObservableCollection<ToolkitRegion> Regions;

        public AccountAndRegionPickerViewModelDesignerData() : base(null)
        {
            Region = new ToolkitRegion() {DisplayName = "Sample Region", Id = "sample-region"};
            Regions = new ObservableCollection<ToolkitRegion>() {Region};
            ConnectionIsValid = false;
            ConnectionIsBad = true;
            ValidationMessage = "This Connection is no good";
        }
    }
}
