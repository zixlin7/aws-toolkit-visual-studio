using System;
using System.Linq;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewVPCsController : FeatureController<ViewVPCsModel>
    {
        ViewVPCsControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewVPCsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshVPCs();
        }


        public void RefreshVPCs()
        {
            var response = this.EC2Client.DescribeVpcs(new DescribeVpcsRequest());

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this.Model.VPCs.Clear();
                foreach (var vpc in response.Vpcs.OrderBy(x => x.VpcId))
                {
                    this.Model.VPCs.Add(new VPCWrapper(vpc));
                }
            }));
        }

        public VPCWrapper CreateVPC()
        {
            var controller = new CreateVPCController();
            var result = controller.Execute(this.EC2Client);
            if (result.Success)
            {
                RefreshVPCs();
                foreach (var vol in Model.VPCs)
                {
                    if (result.FocalName.Equals(vol.VpcId))
                        return vol;
                }
            }
            return null;
        }

        public void DeleteVPC(VPCWrapper vpc)
        {
            var deleteController = new DeleteVPCController();
            var result = deleteController.Execute(this.EC2Client, vpc.VpcId);
            if (result.Success)
            {
                this.Model.VPCs.Remove(vpc);
            }
        }

        public VPCWrapper AssociateDHCPOptionsSet(VPCWrapper vpc)
        {
            var controller = new AssociateDHCPOptionSetController();
            var results = controller.Execute(this.EC2Client, vpc);
            if (results.Success)
            {
                this.RefreshVPCs();

                foreach (var item in this.Model.VPCs)
                {
                    if (string.Equals(item.VpcId, vpc.VpcId))
                        return item;
                }
            }

            return null;
        }

        public void UpdateDNSSettings(VPCWrapper vpc)
        {
            var controller = new UpdateDNSSettingsController();
            controller.Execute(this.EC2Client, vpc);
        }
    }
}
