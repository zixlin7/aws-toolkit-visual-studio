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

namespace Amazon.DevTools
{
    public abstract class AWSDevToolsRequest
    {
        protected const string METHOD = "GIT";
        protected const string SERVICE = "devtools";

        DateTime dateTime;

        public AWSDevToolsRequest()
            : this(DateTime.UtcNow)
        {
        }

        public AWSDevToolsRequest(DateTime dateTime)
        {
            if (dateTime == null)
            {
                throw new ArgumentNullException("dateTime");
            }
            this.dateTime = dateTime.ToUniversalTime();
        }

        public string DateStamp => this.dateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        public string DateTimeStamp => this.dateTime.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);

        public abstract string DerivePath();

        protected internal abstract string DeriveRequest();

        public string Host
        {
            get;
            set;
        }

        public string Region
        {
            get;
            set;
        }

        public string Service => AWSDevToolsRequest.SERVICE;

        protected internal virtual void Validate()
        {
            if (string.IsNullOrEmpty(this.Host))
            {
                throw new InvalidOperationException("[Host]");
            }
            if (string.IsNullOrEmpty(this.Region))
            {
                throw new InvalidOperationException("[Region]");
            }
        }
    }
}
