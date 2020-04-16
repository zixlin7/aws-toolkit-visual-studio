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
    /// PostErrorReport Request Marshaller
    /// </summary>       
    public class PostErrorReportRequestMarshaller : IMarshaller<IRequest, PostErrorReportRequest> , IMarshaller<IRequest,AmazonWebServiceRequest>
    {
        /// <summary>
        /// Marshaller the request object to the HTTP request.
        /// </summary>  
        /// <param name="input"></param>
        /// <returns></returns>
        public IRequest Marshall(AmazonWebServiceRequest input)
        {
            return this.Marshall((PostErrorReportRequest)input);
        }

        /// <summary>
        /// Marshaller the request object to the HTTP request.
        /// </summary>  
        /// <param name="publicRequest"></param>
        /// <returns></returns>
        public IRequest Marshall(PostErrorReportRequest publicRequest)
        {
            IRequest request = new DefaultRequest(publicRequest, "Amazon.ToolkitTelemetry");
            request.Headers["Content-Type"] = "application/json";
            request.Headers[Amazon.Util.HeaderKeys.XAmzApiVersion] = "2017-07-25";            
            request.HttpMethod = "POST";

            request.ResourcePath = "/errorReport";
            request.MarshallerVersion = 2;
            using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                JsonWriter writer = new JsonWriter(stringWriter);
                writer.WriteObjectStart();
                var context = new JsonMarshallerContext(request, writer);
                if(publicRequest.IsSetAWSProduct())
                {
                    context.Writer.WritePropertyName("AWSProduct");
                    context.Writer.Write(publicRequest.AWSProduct);
                }

                if(publicRequest.IsSetAWSProductVersion())
                {
                    context.Writer.WritePropertyName("AWSProductVersion");
                    context.Writer.Write(publicRequest.AWSProductVersion);
                }

                if(publicRequest.IsSetErrorDetails())
                {
                    context.Writer.WritePropertyName("ErrorDetails");
                    context.Writer.WriteObjectStart();

                    var marshaller = ErrorDetailsMarshaller.Instance;
                    marshaller.Marshall(publicRequest.ErrorDetails, context);

                    context.Writer.WriteObjectEnd();
                }

                if(publicRequest.IsSetMetadata())
                {
                    context.Writer.WritePropertyName("Metadata");
                    context.Writer.WriteArrayStart();
                    foreach(var publicRequestMetadataListValue in publicRequest.Metadata)
                    {
                        context.Writer.WriteObjectStart();

                        var marshaller = MetadataEntryMarshaller.Instance;
                        marshaller.Marshall(publicRequestMetadataListValue, context);

                        context.Writer.WriteObjectEnd();
                    }
                    context.Writer.WriteArrayEnd();
                }

                if(publicRequest.IsSetUserdata())
                {
                    context.Writer.WritePropertyName("Userdata");
                    context.Writer.WriteObjectStart();

                    var marshaller = UserdataMarshaller.Instance;
                    marshaller.Marshall(publicRequest.Userdata, context);

                    context.Writer.WriteObjectEnd();
                }

        
                writer.WriteObjectEnd();
                string snippet = stringWriter.ToString();
                request.Content = System.Text.Encoding.UTF8.GetBytes(snippet);
            }


            return request;
        }
        private static PostErrorReportRequestMarshaller _instance = new PostErrorReportRequestMarshaller();        

        internal static PostErrorReportRequestMarshaller GetInstance()
        {
            return _instance;
        }

        /// <summary>
        /// Gets the singleton.
        /// </summary>  
        public static PostErrorReportRequestMarshaller Instance
        {
            get
            {
                return _instance;
            }
        }

    }
}