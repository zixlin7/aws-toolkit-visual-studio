using System;
using System.Collections.Generic;
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
    public class CreateDomainController : BaseContextCommand
    {
        CreateDomainControl _control;
        CreateDomainModel _model;
        SimpleDBRootViewModel _rootModel;
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as SimpleDBRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new CreateDomainModel();
            this._control = new CreateDomainControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public CreateDomainModel Model
        {
            get { return this._model; }
        }

        public void Persist()
        {
            try
            {
                CreateDomainRequest request = new CreateDomainRequest()
                {
                    DomainName = this._model.DomainName
                };
                this._rootModel.SimpleDBClient.CreateDomain(request);

                this._results = new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(this._model.DomainName)
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating domain: " + e.Message);
                this._results = new ActionResults().WithSuccess(false);
            }
        }
    }
}
