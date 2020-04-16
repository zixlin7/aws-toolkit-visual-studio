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
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

using Amazon.ToolkitTelemetry.Model;
using Amazon.ToolkitTelemetry.Model.Internal.MarshallTransformations;
using Amazon.ToolkitTelemetry.Internal;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.Runtime.Internal.Transform;

namespace Amazon.ToolkitTelemetry
{
    /// <summary>
    /// Implementation for accessing ToolkitTelemetry
    ///
    /// 
    /// </summary>
    public partial class AmazonToolkitTelemetryClient : AmazonServiceClient, IAmazonToolkitTelemetry
    {
        private static IServiceMetadata serviceMetadata = new AmazonToolkitTelemetryMetadata();
        #region Constructors

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with the credentials loaded from the application's
        /// default configuration, and if unsuccessful from the Instance Profile service on an EC2 instance.
        /// 
        /// Example App.config with credentials set. 
        /// <code>
        /// &lt;?xml version="1.0" encoding="utf-8" ?&gt;
        /// &lt;configuration&gt;
        ///     &lt;appSettings&gt;
        ///         &lt;add key="AWSProfileName" value="AWS Default"/&gt;
        ///     &lt;/appSettings&gt;
        /// &lt;/configuration&gt;
        /// </code>
        ///
        /// </summary>
        public AmazonToolkitTelemetryClient()
            : base(FallbackCredentialsFactory.GetCredentials(), new AmazonToolkitTelemetryConfig()) { }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with the credentials loaded from the application's
        /// default configuration, and if unsuccessful from the Instance Profile service on an EC2 instance.
        /// 
        /// Example App.config with credentials set. 
        /// <code>
        /// &lt;?xml version="1.0" encoding="utf-8" ?&gt;
        /// &lt;configuration&gt;
        ///     &lt;appSettings&gt;
        ///         &lt;add key="AWSProfileName" value="AWS Default"/&gt;
        ///     &lt;/appSettings&gt;
        /// &lt;/configuration&gt;
        /// </code>
        ///
        /// </summary>
        /// <param name="region">The region to connect.</param>
        public AmazonToolkitTelemetryClient(RegionEndpoint region)
            : base(FallbackCredentialsFactory.GetCredentials(), new AmazonToolkitTelemetryConfig{RegionEndpoint = region}) { }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with the credentials loaded from the application's
        /// default configuration, and if unsuccessful from the Instance Profile service on an EC2 instance.
        /// 
        /// Example App.config with credentials set. 
        /// <code>
        /// &lt;?xml version="1.0" encoding="utf-8" ?&gt;
        /// &lt;configuration&gt;
        ///     &lt;appSettings&gt;
        ///         &lt;add key="AWSProfileName" value="AWS Default"/&gt;
        ///     &lt;/appSettings&gt;
        /// &lt;/configuration&gt;
        /// </code>
        ///
        /// </summary>
        /// <param name="config">The AmazonToolkitTelemetryClient Configuration Object</param>
        public AmazonToolkitTelemetryClient(AmazonToolkitTelemetryConfig config)
            : base(FallbackCredentialsFactory.GetCredentials(), config) { }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with AWS Credentials
        /// </summary>
        /// <param name="credentials">AWS Credentials</param>
        public AmazonToolkitTelemetryClient(AWSCredentials credentials)
            : this(credentials, new AmazonToolkitTelemetryConfig())
        {
        }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with AWS Credentials
        /// </summary>
        /// <param name="credentials">AWS Credentials</param>
        /// <param name="region">The region to connect.</param>
        public AmazonToolkitTelemetryClient(AWSCredentials credentials, RegionEndpoint region)
            : this(credentials, new AmazonToolkitTelemetryConfig{RegionEndpoint = region})
        {
        }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with AWS Credentials and an
        /// AmazonToolkitTelemetryClient Configuration object.
        /// </summary>
        /// <param name="credentials">AWS Credentials</param>
        /// <param name="clientConfig">The AmazonToolkitTelemetryClient Configuration Object</param>
        public AmazonToolkitTelemetryClient(AWSCredentials credentials, AmazonToolkitTelemetryConfig clientConfig)
            : base(credentials, clientConfig)
        {
        }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with AWS Access Key ID and AWS Secret Key
        /// </summary>
        /// <param name="awsAccessKeyId">AWS Access Key ID</param>
        /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
        public AmazonToolkitTelemetryClient(string awsAccessKeyId, string awsSecretAccessKey)
            : this(awsAccessKeyId, awsSecretAccessKey, new AmazonToolkitTelemetryConfig())
        {
        }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with AWS Access Key ID and AWS Secret Key
        /// </summary>
        /// <param name="awsAccessKeyId">AWS Access Key ID</param>
        /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
        /// <param name="region">The region to connect.</param>
        public AmazonToolkitTelemetryClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region)
            : this(awsAccessKeyId, awsSecretAccessKey, new AmazonToolkitTelemetryConfig() {RegionEndpoint=region})
        {
        }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with AWS Access Key ID, AWS Secret Key and an
        /// AmazonToolkitTelemetryClient Configuration object. 
        /// </summary>
        /// <param name="awsAccessKeyId">AWS Access Key ID</param>
        /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
        /// <param name="clientConfig">The AmazonToolkitTelemetryClient Configuration Object</param>
        public AmazonToolkitTelemetryClient(string awsAccessKeyId, string awsSecretAccessKey, AmazonToolkitTelemetryConfig clientConfig)
            : base(awsAccessKeyId, awsSecretAccessKey, clientConfig)
        {
        }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with AWS Access Key ID and AWS Secret Key
        /// </summary>
        /// <param name="awsAccessKeyId">AWS Access Key ID</param>
        /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
        /// <param name="awsSessionToken">AWS Session Token</param>
        public AmazonToolkitTelemetryClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken)
            : this(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, new AmazonToolkitTelemetryConfig())
        {
        }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with AWS Access Key ID and AWS Secret Key
        /// </summary>
        /// <param name="awsAccessKeyId">AWS Access Key ID</param>
        /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
        /// <param name="awsSessionToken">AWS Session Token</param>
        /// <param name="region">The region to connect.</param>
        public AmazonToolkitTelemetryClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, RegionEndpoint region)
            : this(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, new AmazonToolkitTelemetryConfig{RegionEndpoint = region})
        {
        }

        /// <summary>
        /// Constructs AmazonToolkitTelemetryClient with AWS Access Key ID, AWS Secret Key and an
        /// AmazonToolkitTelemetryClient Configuration object. 
        /// </summary>
        /// <param name="awsAccessKeyId">AWS Access Key ID</param>
        /// <param name="awsSecretAccessKey">AWS Secret Access Key</param>
        /// <param name="awsSessionToken">AWS Session Token</param>
        /// <param name="clientConfig">The AmazonToolkitTelemetryClient Configuration Object</param>
        public AmazonToolkitTelemetryClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, AmazonToolkitTelemetryConfig clientConfig)
            : base(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, clientConfig)
        {
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Creates the signer for the service.
        /// </summary>
        protected override AbstractAWSSigner CreateSigner()
        {
            return new AWS4Signer();
        }    

        /// <summary>
        /// Capture metadata for the service.
        /// </summary>
        protected override IServiceMetadata ServiceMetadata
        {
            get
            {
                return serviceMetadata;
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Disposes the service client.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion


        #region  PostErrorReport


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostErrorReport service method.</param>
        /// 
        /// <returns>The response from the PostErrorReport service method, as returned by ToolkitTelemetry.</returns>
        public virtual PostErrorReportResponse PostErrorReport(PostErrorReportRequest request)
        {
            var options = new InvokeOptions();
            options.RequestMarshaller = PostErrorReportRequestMarshaller.Instance;
            options.ResponseUnmarshaller = PostErrorReportResponseUnmarshaller.Instance;

            return Invoke<PostErrorReportResponse>(request, options);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostErrorReport service method.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of cancellation.
        /// </param>
        /// 
        /// <returns>The response from the PostErrorReport service method, as returned by ToolkitTelemetry.</returns>
        public virtual Task<PostErrorReportResponse> PostErrorReportAsync(PostErrorReportRequest request, System.Threading.CancellationToken cancellationToken = default(CancellationToken))
        {
            var options = new InvokeOptions();
            options.RequestMarshaller = PostErrorReportRequestMarshaller.Instance;
            options.ResponseUnmarshaller = PostErrorReportResponseUnmarshaller.Instance;
            
            return InvokeAsync<PostErrorReportResponse>(request, options, cancellationToken);
        }

        #endregion
        
        #region  PostFeedback


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostFeedback service method.</param>
        /// 
        /// <returns>The response from the PostFeedback service method, as returned by ToolkitTelemetry.</returns>
        public virtual PostFeedbackResponse PostFeedback(PostFeedbackRequest request)
        {
            var options = new InvokeOptions();
            options.RequestMarshaller = PostFeedbackRequestMarshaller.Instance;
            options.ResponseUnmarshaller = PostFeedbackResponseUnmarshaller.Instance;

            return Invoke<PostFeedbackResponse>(request, options);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostFeedback service method.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of cancellation.
        /// </param>
        /// 
        /// <returns>The response from the PostFeedback service method, as returned by ToolkitTelemetry.</returns>
        public virtual Task<PostFeedbackResponse> PostFeedbackAsync(PostFeedbackRequest request, System.Threading.CancellationToken cancellationToken = default(CancellationToken))
        {
            var options = new InvokeOptions();
            options.RequestMarshaller = PostFeedbackRequestMarshaller.Instance;
            options.ResponseUnmarshaller = PostFeedbackResponseUnmarshaller.Instance;
            
            return InvokeAsync<PostFeedbackResponse>(request, options, cancellationToken);
        }

        #endregion
        
        #region  PostMetrics


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostMetrics service method.</param>
        /// 
        /// <returns>The response from the PostMetrics service method, as returned by ToolkitTelemetry.</returns>
        public virtual PostMetricsResponse PostMetrics(PostMetricsRequest request)
        {
            var options = new InvokeOptions();
            options.RequestMarshaller = PostMetricsRequestMarshaller.Instance;
            options.ResponseUnmarshaller = PostMetricsResponseUnmarshaller.Instance;

            return Invoke<PostMetricsResponse>(request, options);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostMetrics service method.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of cancellation.
        /// </param>
        /// 
        /// <returns>The response from the PostMetrics service method, as returned by ToolkitTelemetry.</returns>
        public virtual Task<PostMetricsResponse> PostMetricsAsync(PostMetricsRequest request, System.Threading.CancellationToken cancellationToken = default(CancellationToken))
        {
            var options = new InvokeOptions();
            options.RequestMarshaller = PostMetricsRequestMarshaller.Instance;
            options.ResponseUnmarshaller = PostMetricsResponseUnmarshaller.Instance;
            
            return InvokeAsync<PostMetricsResponse>(request, options, cancellationToken);
        }

        #endregion
        
    }
}