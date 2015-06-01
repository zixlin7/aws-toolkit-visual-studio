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
using System.Windows.Forms;
using System.Windows.Forms.Integration;

using Amazon.AWSToolkit.CommonUI;

using log4net;


namespace Amazon.AWSToolkit.Studio
{
    /// <summary>
    /// Interaction logic for PropertiesControl.xaml
    /// </summary>
    public partial class PropertiesControl : BaseAWSControl
    {
        IList _propertyObjects;

        public PropertiesControl(IList propertyObjects)
        {
            this._propertyObjects = propertyObjects;
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                if (this._propertyObjects.Count == 0)
                    return "";
                if (this._propertyObjects.Count == 1 && this._propertyObjects[0] is PropertiesModel.PropertyObject)
                {
                    var prop = this._propertyObjects[0] as PropertiesModel.PropertyObject;
                    return string.Format("{0}: {1}", prop.GetClassName(), prop.GetComponentName());
                }
                else if (this._propertyObjects.Count > 1 && this._propertyObjects[0] is PropertiesModel.PropertyObject)
                {
                    var prop = this._propertyObjects[0] as PropertiesModel.PropertyObject;
                    return string.Format("{0}s", prop.GetClassName());
                }

                return "";
            }
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            var host = new WindowsFormsHost();

            var objects = new object[this._propertyObjects.Count];
            this._propertyObjects.CopyTo(objects, 0);
            PropertyGrid propertyGrid = new PropertyGrid();
            propertyGrid.SelectedObjects = objects;
            propertyGrid.CommandsVisibleIfAvailable = false;
            host.Child = propertyGrid;

            this._ctlMainPanel.Children.Add(host);
        }
    }
}
