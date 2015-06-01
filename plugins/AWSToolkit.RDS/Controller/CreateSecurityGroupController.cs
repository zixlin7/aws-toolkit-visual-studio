using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.View;
using Amazon.AWSToolkit.RDS.Nodes;

using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class CreateSecurityGroupController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSecurityGroupController));

        ActionResults _results;
        IAmazonRDS _rdsClient;
        CreateSecurityGroupModel _model;
        RDSSecurityGroupRootViewModel _securityGroupRootViewModel;


        public CreateSecurityGroupController()
        {
        }

        public CreateSecurityGroupModel Model
        {
            get { return _model; }
        }

        public override ActionResults Execute(IViewModel model)
        {
            var securityGroupRootViewModel = model as RDSSecurityGroupRootViewModel;
            if (securityGroupRootViewModel == null)
                return new ActionResults().WithSuccess(false);

            return this.Execute(securityGroupRootViewModel);
        }

        public ActionResults Execute(RDSSecurityGroupRootViewModel securityGroupRootViewModel)
        {
            this._securityGroupRootViewModel = securityGroupRootViewModel;
            _model = new CreateSecurityGroupModel();
            _rdsClient = this._securityGroupRootViewModel.RDSClient;

            var control = new CreateSecurityGroupControl(this);

            ToolkitFactory.Instance.ShellProvider.ShowModal(control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        public string CreateSecurityGroup()
        {
            var request = new CreateDBSecurityGroupRequest()
            {
                DBSecurityGroupName = this._model.Name,
                DBSecurityGroupDescription = this._model.Description
            };

            var response = this._rdsClient.CreateDBSecurityGroup(request);

            this._results = new ActionResults().WithFocalname(this._model.Name).WithSuccess(true);

            if (this._securityGroupRootViewModel != null)
            {
                var wrapper = new DBSecurityGroupWrapper(response.DBSecurityGroup);
                this._securityGroupRootViewModel.AddSecurityGroup(wrapper);
            }


            return this._model.Name;
        }
    }
}
