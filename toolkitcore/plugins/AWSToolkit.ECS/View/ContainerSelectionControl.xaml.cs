using System.Collections.ObjectModel;
using System.Windows;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Displays a container selection control
    /// </summary>
    public partial class ContainerSelectionControl : BaseAWSControl
    {
        public ContainerSelectionControl()
        {
            InitializeComponent();
        }

        public override string Title => "View CloudWatch Logs for ECS Task";

        public static readonly DependencyProperty ContainersProperty =
            DependencyProperty.Register(
                nameof(Containers), typeof(ObservableCollection<string>), typeof(ContainerSelectionControl),
                new PropertyMetadata(null));


        public static readonly DependencyProperty ContainerProperty =
            DependencyProperty.Register(
                nameof(Container), typeof(string), typeof(ContainerSelectionControl),
                new PropertyMetadata(null));

        /// <summary>
        /// Collection of containers to select from
        /// </summary>
        public ObservableCollection<string> Containers
        {
            get => (ObservableCollection<string>) GetValue(ContainersProperty);
            set => SetValue(ContainersProperty, value);
        }

        /// <summary>
        /// Selected container
        /// </summary>
        public string Container
        {
            get => (string) GetValue(ContainerProperty);
            set => SetValue(ContainerProperty, value);
        }
    }
}
