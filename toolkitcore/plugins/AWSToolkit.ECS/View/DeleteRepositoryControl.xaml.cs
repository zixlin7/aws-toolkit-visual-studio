using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for DeleteRepositoryControl.xaml
    /// </summary>
    public partial class DeleteRepositoryControl : BaseAWSControl
    {
        /// <summary>
        /// Name of the repository to be deleted.
        /// </summary>
        public string RepositoryName { get; }

        public bool ForceDelete { get; set; }

        /// <summary>
        /// Title for the window in which the control is displayed.
        /// </summary>
        public override string Title => "Delete Repository";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="repositoryName">Name of the repository to be deleted.</param>
        public DeleteRepositoryControl(string repositoryName)
        {
            this.RepositoryName = repositoryName;      
            this.DataContext = this;                  
            InitializeComponent();   
        }
    }
}
