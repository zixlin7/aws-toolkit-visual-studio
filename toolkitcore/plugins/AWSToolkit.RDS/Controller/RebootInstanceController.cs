using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.View;
using Amazon.AWSToolkit.EC2.Controller;

using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class RebootInstanceController : BulkChangeController<IAmazonRDS, DBInstanceWrapper>
    {

        public override ActionResults Execute(IViewModel model)
        {
            var rdsInstanceViewModel = model as RDSInstanceViewModel;
            if (rdsInstanceViewModel == null)
                return new ActionResults().WithSuccess(false);

            var list = new List<DBInstanceWrapper>(){rdsInstanceViewModel.DBInstance};
            return base.Execute(rdsInstanceViewModel.RDSClient, list);
        }

        protected override string Action
        {
            get { return "Reboot"; }
        }

        protected override string ConfirmMessage
        {
            get { return "Are you sure you want to reboot this DB Instance(s)?"; }
        }

        protected override void PerformAction(IAmazonRDS rdsClient, DBInstanceWrapper instance)
        {
            rdsClient.RebootDBInstance(new RebootDBInstanceRequest() { DBInstanceIdentifier = instance.DBInstanceIdentifier });
        }
    }
}
