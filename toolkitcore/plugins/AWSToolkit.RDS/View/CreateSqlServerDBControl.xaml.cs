using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
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

        public override string Title => "Create SQL Server Database";

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
