using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Amazon.AWSToolkit.CommonUI.ResourceTags
{
    public class TagGridCellConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.ToString().Equals(ResourceTagsModel.NameKey, StringComparison.OrdinalIgnoreCase))
                return "Silver";

            return Colors.White.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for ResourceTagsControl.xaml
    /// </summary>
    public partial class ResourceTagsControl
    {
        public ResourceTagsControl()
        {
            InitializeComponent();
        }

        private void _ctlTagsGrid_OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            var resourceTag = e.Row.Item as ResourceTag;
            if (resourceTag == null)
                return;

            if (e.Column.DisplayIndex == 0 && resourceTag.IsReadOnlyKey)
            {
                e.Cancel = true;
                _ctlErrMessages.Text = string.Format("The key '{0}' is read-only", resourceTag.Key);
            }
            else
                _ctlErrMessages.Text = string.Empty;
        }
    }
}
