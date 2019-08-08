using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.Auth.AccessControlPolicy;
using Amazon.Lambda.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class EventSourceWrapper : BaseModel
    {
        public enum EventSourceType { Push, Pull };

        public EventSourceType Type { get; }
        public string ServiceName { get; }
        public string ResourceDisplayName { get; }
        public string Details { get; }
        public string UUID { get; }

        public const string DYNAMODB_FRIENDLY_NAME = "DynamoDB";
        public const string KINESIS_FRIENDLY_NAME = "Kinesis";
        public const string SNS_FRIENDLY_NAME = "SNS";
        public const string SQS_FRIENDLY_NAME = "SQS";
        public const string EVENTS_FRIENDLY_NAME = "CloudWatch Events";
        public const string S3_FRIENDLY_NAME = "S3";

        public EventSourceWrapper(EventSourceMappingConfiguration configuration)
        {
            this.Type = EventSourceType.Pull;
            this.UUID = configuration.UUID;

            var tokens = configuration.EventSourceArn.Split(':');

            switch (tokens[2].ToLowerInvariant())
            {
                case "kinesis":
                    ServiceName = KINESIS_FRIENDLY_NAME;
                    break;
                case "dynamodb":
                    ServiceName = DYNAMODB_FRIENDLY_NAME;
                    break;
                case "sqs":
                    ServiceName = SQS_FRIENDLY_NAME;
                    break;
                default:
                    ServiceName = tokens[2];
                    break;
            }

            if (string.Equals(ServiceName, DYNAMODB_FRIENDLY_NAME))
            {
                var streamTokens = configuration.EventSourceArn.Split('/');
                this.ResourceDisplayName = streamTokens[1];
            }
            else
            {
                this.ResourceDisplayName = tokens[tokens.Length - 1];
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Batch Size: {0}", configuration.BatchSize);
            if(!string.IsNullOrEmpty(configuration.LastProcessingResult))
            {
                sb.AppendFormat(", Last Result: {0}", configuration.LastProcessingResult);
            }
            this.Details = sb.ToString();
        }

        public EventSourceWrapper(Statement statement)
        {
            this.Type = EventSourceType.Push;
            this.UUID = statement.Id;

            if (statement.Principals.Count > 0)
            {
                if (statement.Principals[0].Id.StartsWith("s3", StringComparison.InvariantCultureIgnoreCase))
                    ServiceName = S3_FRIENDLY_NAME;
                else if (statement.Principals[0].Id.StartsWith("sns", StringComparison.InvariantCultureIgnoreCase))
                    ServiceName = SNS_FRIENDLY_NAME;
                else if (statement.Principals[0].Id.StartsWith("sqs", StringComparison.InvariantCultureIgnoreCase))
                    ServiceName = SQS_FRIENDLY_NAME;
                else if (statement.Principals[0].Id.StartsWith("events", StringComparison.InvariantCultureIgnoreCase))
                    ServiceName = EVENTS_FRIENDLY_NAME;
                else
                    ServiceName = statement.Principals[0].Provider;
            }


            if (string.Equals(ServiceName, DYNAMODB_FRIENDLY_NAME))
            {
                var tokens = statement.Conditions[0].Values[0].Split('/');
                this.ResourceDisplayName = tokens[1];
            }
            else
            {
                var tokens = statement.Conditions[0].Values[0].Split(':');
                this.ResourceDisplayName = tokens[tokens.Length - 1];
            }

            this.Details = "Action: " + statement.Actions[0].ActionName;
        }

        public System.Windows.Media.ImageSource ServiceIcon
        {
            get
            {
                switch (this.ServiceName)
                {
                    case DYNAMODB_FRIENDLY_NAME:
                        return IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.dynamodb-service.png").Source;
                    case KINESIS_FRIENDLY_NAME:
                        return IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.kinesis-service.png").Source;
                    case S3_FRIENDLY_NAME:
                        return IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.s3-service.png").Source;
                    case SNS_FRIENDLY_NAME:
                        return IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.sns-service.png").Source;
                    case SQS_FRIENDLY_NAME:
                        return IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.sqs-service.png").Source;
                    case EVENTS_FRIENDLY_NAME:
                        return IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.cloudwatch-service.png").Source;
                    default:
                        return null;
                }
            }
        }
    }
}
