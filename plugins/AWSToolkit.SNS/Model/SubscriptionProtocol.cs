using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class SubscriptionProtocol
    {
        public static readonly SubscriptionProtocol HTTP = new SubscriptionProtocol("HTTP", "http");
        public static readonly SubscriptionProtocol HTTPS = new SubscriptionProtocol("HTTPS", "https");
        public static readonly SubscriptionProtocol EMAIL = new SubscriptionProtocol("Email", "email");
        public static readonly SubscriptionProtocol EMAIL_JSON = new SubscriptionProtocol("Email (JSON)", "email-json");
        public static readonly SubscriptionProtocol SQS = new SubscriptionProtocol("Amazon SQS", "sqs");
        public static readonly SubscriptionProtocol SMS = new SubscriptionProtocol("SMS", "sms");
        public static readonly SubscriptionProtocol LAMBDA = new SubscriptionProtocol("Lambda", "lambda");

        public static readonly SubscriptionProtocol[] ALL_PROTOCOLS = { HTTP, HTTPS, EMAIL, EMAIL_JSON, SQS, LAMBDA, SMS };

        string _displayName;
        string _systemName;

        private SubscriptionProtocol(string displayName, string systemName)
        {
            this._displayName = displayName;
            this._systemName = systemName;
        }

        public string DisplayName
        {
            get { return this._displayName; }
        }

        public string SystemName
        {
            get { return this._systemName; }
        }

        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}
