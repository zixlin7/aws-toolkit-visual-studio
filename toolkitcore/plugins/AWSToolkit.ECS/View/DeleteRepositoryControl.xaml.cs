using Amazon.AWSToolkit.CommonUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for DeleteRepositoryControl.xaml
    /// </summary>
    public partial class DeleteRepositoryControl : BaseAWSControl
    {
        /// <summary>
        /// Name if the repository to be deleted.
        /// </summary>
        public string RepositoryName { get; private set; }

        public bool ForceDelete { get; set; }

        /// <summary>
        /// Title for the window in which the control is displayed.
        /// </summary>
        public override string Title
        {
            get
            {
                return "Delete Repository";
            }
        }

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
