using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        public string ClusterName { get; private set; }

        /// <summary>
        /// Title for the window in which the control is displayed.
        /// </summary>
        public override string Title
        {
            get
            {
                return "Delete Cluster";
            }
        }

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
