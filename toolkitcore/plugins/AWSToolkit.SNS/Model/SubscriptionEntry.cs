using System.ComponentModel;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class SubscriptionEntry : PropertiesModel
    {
        [DisplayName("Topic Arn")]
        [ReadOnly(true)]
        public string TopicArn
        {
            get;
            set;
        }

        [DisplayName("Protocol")]
        [ReadOnly(true)]
        public string Protocol
        {
            get;
            set;
        }

        [DisplayName("End Point")]
        [ReadOnly(true)]
        public string EndPoint
        {
            get;
            set;
        }

        [DisplayName("Subscription ID")]
        [ReadOnly(true)]
        public string SubscriptionId
        {
            get;
            set;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Subscription";
            componentName = this.SubscriptionId;
        }   
    }
}
