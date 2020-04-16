/*
 * Copyright 2010-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

/*
 * Do not modify this file. This file is generated from the telemetry-2017-07-25.normal.json service model.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;

using Amazon.ToolkitTelemetry.Model;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Transform;
using Amazon.Runtime.Internal.Util;
using ThirdParty.Json.LitJson;

namespace Amazon.ToolkitTelemetry.Model.Internal.MarshallTransformations
{
    /// <summary>
    /// Userdata Marshaller
    /// </summary>       
    public class UserdataMarshaller : IRequestMarshaller<Userdata, JsonMarshallerContext> 
    {
        /// <summary>
        /// Unmarshaller the response from the service to the response class.
        /// </summary>  
        /// <param name="requestObject"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public void Marshall(Userdata requestObject, JsonMarshallerContext context)
        {
            if(requestObject.IsSetComment())
            {
                context.Writer.WritePropertyName("Comment");
                context.Writer.Write(requestObject.Comment);
            }

            if(requestObject.IsSetEmail())
            {
                context.Writer.WritePropertyName("Email");
                context.Writer.Write(requestObject.Email);
            }

        }

        /// <summary>
        /// Singleton Marshaller.
        /// </summary>  
        public readonly static UserdataMarshaller Instance = new UserdataMarshaller();

    }
}