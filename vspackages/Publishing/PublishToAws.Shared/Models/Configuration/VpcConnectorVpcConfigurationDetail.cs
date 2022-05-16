using System;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    // TODO This is a temporary class until deploy tool validates that "existing VPC" is not empty when using an existing VPC Connector 
    public class VpcConnectorVpcConfigurationDetail : ConfigurationDetail
    {
        public VpcConnectorVpcConfigurationDetail()
        {
            PropertyChanged += VpcConnectorVpcConfigurationDetail_PropertyChanged;
        }

        private void VpcConnectorVpcConfigurationDetail_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Visible):
                    Validate();        
                    break;
                case nameof(Value):
                    Validate();        
                    break;
            }
        }

        private void Validate()
        {
            if (Visible && (!(Value is string valueStr) || string.IsNullOrWhiteSpace(valueStr)))
            {
                ValidationMessage = "A VPC must be selected";
                return;
            }

            ValidationMessage = string.Empty;
        }
    }
}
