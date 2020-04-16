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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Amazon.Runtime;
using Amazon.ToolkitTelemetry.Model;

namespace Amazon.ToolkitTelemetry
{
    /// <summary>
    /// Interface for accessing ToolkitTelemetry
    ///
    /// 
    /// </summary>
    public partial interface IAmazonToolkitTelemetry : IAmazonService, IDisposable
    {

        
        #region  PostErrorReport


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostErrorReport service method.</param>
        /// 
        /// <returns>The response from the PostErrorReport service method, as returned by ToolkitTelemetry.</returns>
        PostErrorReportResponse PostErrorReport(PostErrorReportRequest request);



        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostErrorReport service method.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of cancellation.
        /// </param>
        /// 
        /// <returns>The response from the PostErrorReport service method, as returned by ToolkitTelemetry.</returns>
        Task<PostErrorReportResponse> PostErrorReportAsync(PostErrorReportRequest request, CancellationToken cancellationToken = default(CancellationToken));

        #endregion
        
        #region  PostFeedback


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostFeedback service method.</param>
        /// 
        /// <returns>The response from the PostFeedback service method, as returned by ToolkitTelemetry.</returns>
        PostFeedbackResponse PostFeedback(PostFeedbackRequest request);



        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostFeedback service method.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of cancellation.
        /// </param>
        /// 
        /// <returns>The response from the PostFeedback service method, as returned by ToolkitTelemetry.</returns>
        Task<PostFeedbackResponse> PostFeedbackAsync(PostFeedbackRequest request, CancellationToken cancellationToken = default(CancellationToken));

        #endregion
        
        #region  PostMetrics


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostMetrics service method.</param>
        /// 
        /// <returns>The response from the PostMetrics service method, as returned by ToolkitTelemetry.</returns>
        PostMetricsResponse PostMetrics(PostMetricsRequest request);



        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Container for the necessary parameters to execute the PostMetrics service method.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of cancellation.
        /// </param>
        /// 
        /// <returns>The response from the PostMetrics service method, as returned by ToolkitTelemetry.</returns>
        Task<PostMetricsResponse> PostMetricsAsync(PostMetricsRequest request, CancellationToken cancellationToken = default(CancellationToken));

        #endregion
        
    }
}