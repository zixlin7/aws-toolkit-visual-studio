using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Amazon.AWSToolkit.EC2.Controller;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for CreateKeyPairResponseControl.xaml
    /// </summary>
    public partial class CreateKeyPairResponseControl : BaseAWSControl
    {
        CreateKeyPairController _controller;

        public CreateKeyPairResponseControl(CreateKeyPairController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Create Key Pair";}
        }

        void onExport(object sender, RoutedEventArgs evnt)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = this._controller.Model.KeyPairName; // Default file name
                dlg.DefaultExt = ".pem"; // Default file extension
                dlg.Filter = "PEM File (.pem)|*.pem"; // Filter files by extension

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    string filename = dlg.FileName;
                    using (StreamWriter writer = new StreamWriter(filename))
                    {
                        writer.Write(this._controller.Model.PrivateKey);
                    }
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving private key: " + e.Message);
            }
        }

        public override bool OnCommit()
        {
            try
            {
                if(this._controller.Model.StorePrivateKey)
                    this._controller.PersistPrivateKey();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving private key: " + e.Message);
            }
            return true;
        }
    }
}
