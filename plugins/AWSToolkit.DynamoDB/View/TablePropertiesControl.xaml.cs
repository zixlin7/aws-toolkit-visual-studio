using System;
using System.Linq;
using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.DynamoDB.Controller;
using Amazon.AWSToolkit.DynamoDB.Model;

namespace Amazon.AWSToolkit.DynamoDB.View
{
    /// <summary>
    /// Interaction logic for TablePropertiesControl.xaml
    /// </summary>
    public partial class TablePropertiesControl : BaseAWSControl
    {
        TablePropertiesController _controller;

        public TablePropertiesControl(TablePropertiesController controller)
        {
            this._controller = controller;
            InitializeComponent();

            this._ctlGlobalIndexes.Mode = Components.TableIndexesControl.EditingMode.GlobalModified;
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
        }

        protected override object LoadAndReturnModel()
        {   
            return this._controller.Model;
        }

        internal void PreloadModel()
        {
            _controller.LoadModel();
        }

        public override string Title
        {
            get
            {
                return string.Format("Properties: {0}", this._controller.Model.TableName);
            }
        }

        public override bool Validated()
        {
            int value;
            if (!int.TryParse(this._controller.Model.ReadCapacityUnits, out value) || value <= 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Read Capacity is a required field.");
                return false;
            }
            if (!int.TryParse(this._controller.Model.WriteCapacityUnits, out value) || value <= 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Write Capacity is a required field.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            return this._controller.Persist();
        }
    }
}
