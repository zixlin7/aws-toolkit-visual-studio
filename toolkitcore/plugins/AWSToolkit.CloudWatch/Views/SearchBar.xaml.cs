using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CloudWatch.Core;

namespace Amazon.AWSToolkit.CloudWatch.Views
{
    /// <summary>
    /// Interaction logic for SearchBar.xaml
    /// </summary>
    public partial class SearchBar : UserControl
    {
        public SearchBar()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(!(e.NewValue is ILogSearchProperties))
            {
                throw new System.InvalidOperationException($"Did not receive expected type: {nameof(ILogSearchProperties)}");
            }
        }
    }
}
