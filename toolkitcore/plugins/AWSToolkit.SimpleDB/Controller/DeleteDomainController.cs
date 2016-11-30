using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SimpleDB.Nodes;
using Amazon.AWSToolkit;

using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;

namespace Amazon.AWSToolkit.SimpleDB.Controller
{
    public class DeleteDomainController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            SimpleDBDomainViewModel domainModel = model as SimpleDBDomainViewModel;
            if (domainModel == null)
                return new ActionResults().WithSuccess(false);

            long itemCount = getNumberOfItems(domainModel.SimpleDBClient, domainModel.Name);
            string msg;
            if (itemCount == 0)
                msg = string.Format("Are you sure you want to delete the {0} domain?", model.Name);
            else
                msg = string.Format("Domain {0} has {1} item(s), are you sure you want to delete this domain?", model.Name, itemCount);

            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Domain", msg))
            {
                try
                {
                    var request = new DeleteDomainRequest()
                    {
                        DomainName = model.Name
                    };
                    domainModel.SimpleDBClient.DeleteDomain(request);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting domain: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }

            return new ActionResults().WithSuccess(false);
        }

        public long getNumberOfItems(IAmazonSimpleDB sdbClient, string domainName)
        {
            DomainMetadataResponse response = sdbClient.DomainMetadata(new DomainMetadataRequest() { DomainName = domainName });
            long items = response.ItemCount;
            return items;
        }
    }
}
