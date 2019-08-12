using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class CreateSubscriptionModel : BaseModel
    {
        string _region;
        string _topicArn;
        SubscriptionProtocol _protocol;
        string _endpoint;
        List<string> _possibleTopicArns = new List<string>();
        List<string> _possibleSQSEndpoints = new List<string>();
        List<string> _possibleLambdaEndpoints = new List<string>();
        bool _isTopicARNReadOnly;

        public CreateSubscriptionModel(string region)
        {
            this._region = region;
            this.Protocol = this.PossibleProtocol[0];
        }

        public bool IsTopicARNReadOnly
        {
            get => this._isTopicARNReadOnly;
            set
            {
                this._isTopicARNReadOnly = value;
                this.NotifyPropertyChanged("IsTopicARNReadOnly");
            }
        }

        bool _isAddSQSPermission = true;
        public bool IsAddSQSPermission
        {
            get => this._isAddSQSPermission;
            set
            {
                this._isAddSQSPermission = value;
                this.NotifyPropertyChanged("IsAddSQSPermission");
            }
        }

        public string TopicArn
        {
            get => this._topicArn;
            set
            {
                this._topicArn = value;
                NotifyPropertyChanged("TopicArn");
            }
        }

        public List<string> PossibleTopicArns => this._possibleTopicArns;

        public List<string> PossibleSQSEndpoints => this._possibleSQSEndpoints;

        public List<string> PossibleLambdaEndpoints => this._possibleLambdaEndpoints;

        public SubscriptionProtocol Protocol
        {
            get => this._protocol;
            set
            {
                this._protocol = value;
                NotifyPropertyChanged("Protocol");
            }
        }

        public SubscriptionProtocol[] PossibleProtocol
        {
            get 
            {
                if (!string.Equals(this._region, RegionEndPointsManager.US_EAST_1))
                {
                    var list = new List<SubscriptionProtocol>();
                    foreach (var prot in SubscriptionProtocol.ALL_PROTOCOLS)
                    {
                        if (prot != SubscriptionProtocol.SMS)
                        {
                            list.Add(prot);
                        }
                    }

                    return list.ToArray();
                }
                return SubscriptionProtocol.ALL_PROTOCOLS; 
            }
        }

        public string FormattedEndpoint
        {
            get
            {
                if (this.Protocol == SubscriptionProtocol.SMS)
                {
                    var end = this.Endpoint.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                    long i;
                    if (!long.TryParse(end, out i) || end.Length < 10 || end.Length > 12)
                    {
                        throw new ApplicationException("Endpoint for SMS is not a valid phone number");
                    }

                    if (end.Length == 10)
                        return "1" + end;
                    else
                        return end;
                }

                return this.Endpoint;
            }
        }


        public string Endpoint
        {
            get => this._endpoint;
            set
            {
                this._endpoint = value;
                NotifyPropertyChanged("Endpoint");
            }
        }
    }
}
