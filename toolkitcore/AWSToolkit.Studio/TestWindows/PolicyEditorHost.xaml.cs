using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Amazon.Auth.AccessControlPolicy;

namespace Amazon.AWSToolkit.Studio.TestWindows
{
    /// <summary>
    /// Interaction logic for PolicyEditorHost.xaml
    /// </summary>
    public partial class PolicyEditorHost : Window
    {
        string samplePolicy =
                "{" +
                "    \"Version\": \"2008-10-17\"," +
                "    \"Id\": \"S3PolicyId1\"," +
                "    \"Statement\": [" +
                "        {" +
                "            \"Sid\":\"IPAllow\"," +
                "            \"Effect\": \"Allow\"," +
                "            \"Principal\": {" +
                "                \"AWS\": \"*\"" +
                "            }," +
                "            \"Action\": \"s3:*\"," +
                "            \"Resource\": \"arn:aws:s3:::bucket/*\"," +
                "            \"Condition\" : {" +
                "                \"IpAddress\" : {" +
                "	                \"aws:SourceIp\":\"192.168.143.0/24\"" +
                "                }," +
                "                \"NotIpAddress\" : {" +
                "                   \"aws:SourceIp\":\"192.168.143.188/32\"" +
                "                }" +
                "            }" +
                "        }," +
                "        {" +
                "            \"Sid\":\"IPDeny\"," +
                "            \"Effect\": \"Deny\"," +
                "            \"Principal\": {" +
                "                \"AWS\": [\"1-22-333-4444\",\"5-66-777-8888\"]" +
                "            }," +
                "            \"Action\": [\"sqs:SendMessage\",\"sqs:ReceiveMessage\"]," +
                "            \"Resource\": [\"sqs:/queue1\",\"sqs:/queue2\"],"+
                "            \"Condition\" : {" +
                "                \"IpAddress\" : {" +
                "                    \"aws:SourceIp\":\"10.1.2.0/24\"" +
                "                }" +
                "            }" +
                "        }" +
                "    ]" +
                "}";

        public PolicyEditorHost()
        {
            this._policy = Policy.FromJson(samplePolicy);
            this.DataContext = this;

            InitializeComponent();

//            this._editor.LoadPolicy(samplePolicy);
        }

        Policy _policy;
        public Policy Policy
        {
            get { return this._policy; }
            set { this._policy = value; }
        }
    }
}
