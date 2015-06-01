using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Lambda.View.DesignData
{
    public class EventSourcesMock : List<EventSourceMock>
    {
        public EventSourcesMock()
        {
            this.Add(new EventSourceMock
            {
                ServiceName = EventSourceMock.KINESIS_FRIENDLY_NAME,
                ResourceDisplayName = "MyStream",
                RoleDisplayName = "invoke_role_lambda"
            });

            this.Add(new EventSourceMock
            {
                ServiceName = EventSourceMock.DYNAMODB_FRIENDLY_NAME,
                ResourceDisplayName = "DynamoDBStream",
                RoleDisplayName = "invoke_role_lambda"
            });
        }

        public ICollection<EventSourceMock> EventSources
        {
            get { return this; }
        }

        public EventSourceMock SelectedEventSource { get; set; }
    }

    public class EventSourceMock
    {
        public const string DYNAMODB_FRIENDLY_NAME = "DynamoDB";
        public const string KINESIS_FRIENDLY_NAME = "Kinesis";

        public string ServiceName { get; set; }
        public string ResourceDisplayName { get; set; }
        public string RoleDisplayName { get; set; }

        public System.Windows.Controls.Image ServiceIcon
        {
            get
            {
                switch(this.ServiceName)
                {
                    case DYNAMODB_FRIENDLY_NAME:
                        return IconHelper.GetIcon("Amazon.AWSToolkit.DynamoDB.Resources.EmbeddedImages.table.png");
                    case KINESIS_FRIENDLY_NAME:
                        return IconHelper.GetIcon("Amazon.AWSToolkit.DynamoDB.Resources.EmbeddedImages.table.png");;
                    default:
                        return null;
                }
            }
        }
            
    }
}
