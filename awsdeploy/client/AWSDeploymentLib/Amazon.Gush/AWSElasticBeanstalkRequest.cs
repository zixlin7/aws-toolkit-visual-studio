/*
 * Copyright 2011-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */
using System;
using System.Globalization;
using System.Text;

namespace Amazon.DevTools
{
    public class AWSElasticBeanstalkRequest : AWSDevToolsRequest
    {
        public AWSElasticBeanstalkRequest()
            : base()
        {
        }

        public AWSElasticBeanstalkRequest(DateTime dateTime)
            : base(dateTime)
        {
        }

        public string Application
        {
            get;
            set;
        }

        public override string DerivePath()
        {
            this.Validate();

            string path = null;
            StringBuilder encodedApplication = new StringBuilder();
            
            foreach (byte b in new UTF8Encoding().GetBytes(this.Application))
            {
                encodedApplication.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            
            if (string.IsNullOrEmpty(this.Environment))
            {
                path = string.Format("/repos/{0}", encodedApplication.ToString());
            }
            else
            {
                path = string.Format("/repos/{0}/{1}", encodedApplication.ToString(), this.Environment);
            }
            return path;
        }

        protected internal override string DeriveRequest()
        {
            this.Validate();

            string path = this.DerivePath();
            string request = string.Format("{0}\n{1}\n\nhost:{2}\n\nhost\n", AWSDevToolsRequest.METHOD, path, this.Host);
            return request;
        }

        public string Environment
        {
            get;
            set;
        }

        protected internal override void Validate()
        {
            base.Validate();
            if (string.IsNullOrEmpty(this.Application))
            {
                throw new InvalidOperationException("[Application]");
            }
            if (string.IsNullOrEmpty(this.Host))
            {
                throw new InvalidOperationException("[Host]");
            }
        }
    }
}
