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

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;

using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for CreateSqlServerDBControl.xaml
    /// </summary>
    public partial class CreateSqlServerDBControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSqlServerDBControl));

        public CreateSqlServerDBControl()
        {
            InitializeComponent();
        }

        CreateSqlServerDBController _controller;

        public CreateSqlServerDBControl(CreateSqlServerDBController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Create SQL Server Database"; }
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.CreateSqlServerDatabase();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating SQL Server database", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating SQL Server database: " + e.Message);
                return false;
            }
        }

        // gets around not being able to use Password property as a data binding target
        private void _password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this._controller.Model.Password = _password.Password;
        }
    }
}
