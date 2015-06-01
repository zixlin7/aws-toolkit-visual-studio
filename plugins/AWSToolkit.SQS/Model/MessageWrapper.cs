using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;

using Amazon.SQS.Model;

namespace Amazon.AWSToolkit.SQS.Model
{
    public class MessageWrapper : PropertiesModel
    {
        Message _message;
        bool _isBase64Encoded;

        public MessageWrapper(Message message)
        {
            this._message = message;

            // If the body starts with ew, which could be a base64 encoded '{' and with an = which is the null terminate 
            // the assume the body is base64 encoded json string.
            if (this._message.Body.StartsWith("ew") && this._message.Body.EndsWith("="))
                this._isBase64Encoded = true;
        }

        [DisplayName("Message ID")]
        public string MessageId
        {
            get { return this._message.MessageId; }
        }

        [Browsable(false)]
        public bool IsBase64Encoded
        {
            get { return this._isBase64Encoded; }
            set
            {
                this._isBase64Encoded = value;
                base.NotifyPropertyChanged("IsBase64Encoded");
                base.NotifyPropertyChanged("Body");
            }
        }

        [DisplayName("Body")]
        public string Body
        {
            get 
            {
                if(this._isBase64Encoded)
                    return StringUtils.DecodeFrom64(this._message.Body);

                return this._message.Body;
            }
        }

        [Browsable(false)]
        public string CleanBody
        {
            get { return cleanBody(this.Body); }
        }

        private string cleanBody(string body)
        {
            return body.Replace("\r\n", " ").Replace("\n", " ");
        }

        [DisplayName("Receipt Handle")]
        public string ReceiptHandle
        {
            get { return this._message.ReceiptHandle; }
        }

        [DisplayName("Sender ID")]
        public string SenderId
        {
            get
            {
                var attribute = getAttribute("SenderId");
                if (attribute == null || string.IsNullOrEmpty(attribute))
                    return null;
                return attribute;
            }
        }

        [DisplayName("Sent Timestamp")]
        public DateTime FormattedSentTimestamp
        {
            get
            {
                var attribute = getAttribute("SentTimestamp");
                if (attribute == null || string.IsNullOrEmpty(attribute))
                    return DateTime.MinValue;

                long value;
                long.TryParse(attribute, out value);

                return Amazon.Util.AWSSDKUtils.ConvertFromUnixEpochSeconds((int)(value/1000));
            }
        }

        string getAttribute(string name)
        {
            string value;
            this._message.Attributes.TryGetValue(name, out value);
            return value;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Message";
            componentName = this.MessageId;
        }   
    }
}
