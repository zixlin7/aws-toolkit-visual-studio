using System;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AllocateAddressControl.xaml
    /// </summary>
    public partial class AllocateAddressControl : BaseAWSControl
    {
        public string Domain { get; private set; }

        public AllocateAddressControl()
        {
            InitializeComponent();
        }

        public override string Title => "Allocate New Address";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._ctlType.Text))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("You must specify how the address will be used.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            Domain = string.Equals("VPC", _ctlType.Text, StringComparison.InvariantCultureIgnoreCase)
                ? AddressWrapper.DOMAIN_VPC
                : AddressWrapper.DOMAIN_EC2;

            return true;
        }
    }
}
