using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SimpleDB.Nodes;
using Amazon.AWSToolkit.SimpleDB.View;
using Amazon.AWSToolkit.SimpleDB.Model;
using Amazon.AWSToolkit;

using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;

namespace Amazon.AWSToolkit.SimpleDB.Controller
{
    public class DomainPropertiesController : BaseContextCommand
    {
        IAmazonSimpleDB _simpleDBClient;
        DomainPropertiesModel _model;



        public override ActionResults Execute(IViewModel model)
        {
            SimpleDBDomainViewModel viewModel = model as SimpleDBDomainViewModel;
            if (viewModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new DomainPropertiesModel(viewModel.Domain);
            this._simpleDBClient = viewModel.SimpleDBClient;
            DomainPropertiesControl control = new DomainPropertiesControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.OK);
            return new ActionResults().WithSuccess(true);
        }

        public DomainPropertiesModel Model
        {
            get { return this._model; }
        }

        public void LoadModel()
        {
            var response = this._simpleDBClient.DomainMetadata(
                new DomainMetadataRequest(){DomainName = this._model.Domain});

            this._model.AttributeNameCount = response.AttributeNameCount.ToString("#,0");
            this._model.AttributeNamesSizeBytes = response.AttributeNamesSizeBytes.ToString("#,0") + " bytes";
            this._model.AttributeValueCount = response.AttributeValueCount.ToString("#,0");
            this._model.AttributeValuesSizeBytes = response.AttributeValuesSizeBytes.ToString("#,0") + " bytes";
            this._model.ItemCount = response.ItemCount.ToString("#,0");
            this._model.ItemNamesSizeBytes = response.ItemNamesSizeBytes.ToString("#,0") + " bytes";
        }
    }
}
