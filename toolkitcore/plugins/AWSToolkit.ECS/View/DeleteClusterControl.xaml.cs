using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for DeleteClusterControl.xaml
    /// </summary>
    public partial class DeleteClusterControl : BaseAWSControl
    {
        /// <summary>
        /// Name of the cluster to be deleted.
        /// </summary>
        public string ClusterName { get; }

        /// <summary>
        /// Title for the window in which the control is displayed.
        /// </summary>
        public override string Title => "Delete Cluster";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="repositoryName">Name of the repository to be deleted.</param>
        public DeleteClusterControl(string clusterName)
        {
            this.ClusterName = clusterName;
            this.DataContext = this;
            InitializeComponent();
        }
    }
}
