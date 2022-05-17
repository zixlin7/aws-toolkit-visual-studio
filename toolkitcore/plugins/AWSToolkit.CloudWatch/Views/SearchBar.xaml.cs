using System.Windows;
using System.Windows.Controls;

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
        }

        public static readonly DependencyProperty FilterTextProperty =
           DependencyProperty.Register(
               nameof(FilterText), typeof(string), typeof(SearchBar),
               new PropertyMetadata(null));

        public static readonly DependencyProperty HintTextProperty =
         DependencyProperty.Register(
             nameof(HintText), typeof(string), typeof(SearchBar),
             new PropertyMetadata("Search by prefix"));

        public string FilterText
        {
            get => (string) GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }

        public string HintText
        {
            get => (string) GetValue(HintTextProperty);
            set => SetValue(HintTextProperty, value);
        }
    }
}
