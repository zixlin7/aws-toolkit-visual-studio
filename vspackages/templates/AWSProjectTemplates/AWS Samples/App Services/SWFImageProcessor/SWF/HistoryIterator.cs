/*******************************************************************************
* Copyright 2009-2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/
using System;
using System.Collections.Generic;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;


namespace $safeprojectname$.SWF
{
    /// <summary>
    /// Implements an IEnumerable for events in the execution history
    /// </summary>
    class HistoryIterator : IEnumerable<HistoryEvent>
    {
        DecisionTask lastResponse;
        IAmazonSimpleWorkflow swfClient;

        /// <summary>
        /// Create a new HistoryIterator to enumerate the history events in the DecisionTask passed in.
        /// </summary>
        /// <param name="client">SWF client to use for getting next page of history events.</param>
        /// <param name="response">The decision task returned from the PollForDecisionTask call.</param>
        public HistoryIterator(IAmazonSimpleWorkflow client, DecisionTask response)
        {
            this.swfClient = client;
            this.lastResponse = response;
        }

        /// <summary>
        /// Creates an enumerator for the history events. Automatically retrieves pages of
        /// history from SWF.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<HistoryEvent> GetEnumerator()
        {
            //Yield the history events in the current page of events
            foreach (HistoryEvent e in lastResponse.Events)
            {
                yield return e;
            }
            //If the NextPageToken is not null, get the next page of history events 
            while (!String.IsNullOrEmpty(lastResponse.NextPageToken))
            {
                List<HistoryEvent> events = GetNextPage();
                //Start yielding results from the next page of events
                foreach (HistoryEvent e in events)
                {
                    yield return e;
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Helper method to call PollForDecisionTask with the NextPageToken
        /// to retrieve the next page of history events.
        /// </summary>
        /// <returns>List of history events received in the next page.</returns>
        List<HistoryEvent> GetNextPage()
        {
            PollForDecisionTaskRequest request = new PollForDecisionTaskRequest()
            {
                Domain = Constants.ImageProcessingDomain,
                NextPageToken = lastResponse.NextPageToken,
                TaskList = new TaskList()
                {
                    Name = Constants.ImageProcessingTaskList
                }
            };

            //AmazonSimpleWorkflow client does exponential back off and retries by default.
            //We want additional retries for robustness in case of transient failures like throttling.
            int retryCount = 10;
            int currentTry = 1;
            bool pollFailed;
            do
            {
                pollFailed = false;
                try
                {
                    this.lastResponse = swfClient.PollForDecisionTask(request).DecisionTask;
                }
                catch (Exception ex)
                {
                    //Swallow exception and keep polling
                    Console.Error.WriteLine("Poll request failed with exception :" + ex);
                    pollFailed = true;
                }
            }
            while (pollFailed && ++currentTry <= retryCount);
            return this.lastResponse.Events;
        }
    }
}
