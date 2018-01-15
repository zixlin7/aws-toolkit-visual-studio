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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;

namespace $safeprojectname$.SWF
{
    /// <summary>
    /// This class acts as the decider for the workflow. It polls the image processing task list and figures out what thumbnails need to be generated.
    /// For each image sent threw the workflow a 256x256, 128x128, 64x64, 32x32 and 16x16 versions of the image will be created.
    /// </summary>
    public class ImageProcessWorkflowWorker
    {
        IAmazonSimpleWorkflow _swfClient = new AmazonSimpleWorkflowClient();
        Task _task;
        CancellationToken _cancellationToken;
        VirtualConsole _console;

        public ImageProcessWorkflowWorker(VirtualConsole console)
        {
            this._console = console;
        }

        /// <summary>
        /// Kick off the worker to poll and process decision tasks
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            this._cancellationToken = cancellationToken;
            this._task = Task.Run((Action)this.PollAndDecide);
        }

        /// <summary>
        /// Polls for descision tasks and decides what decisions to make.
        /// </summary>
        void PollAndDecide()
        {
            this._console.WriteLine("Image Process Workflow Started");
            while (!_cancellationToken.IsCancellationRequested)
            {
                DecisionTask task = Poll();
                if (!string.IsNullOrEmpty(task.TaskToken))
                {
                    //Create the next set of decision based on the current state and 
                    //the execution history
                    List<Decision> decisions = Decide(task);

                    //Complete the task with the new set of decisions
                    CompleteTask(task.TaskToken, decisions);
                }
                //Sleep to avoid aggressive polling
                Thread.Sleep(200);
            }
        }


        /// <summary>
        /// Helper method to poll for decision task from the image processing task list.
        /// </summary>
        /// <returns>Decision task returned from the long poll</returns>
        DecisionTask Poll()
        {
            this._console.WriteLine("Polling for decision task ...");
            PollForDecisionTaskRequest request = new PollForDecisionTaskRequest()
            {
                Domain = Constants.ImageProcessingDomain,
                TaskList = new TaskList()
                {
                    Name = Constants.ImageProcessingTaskList
                }
            };
            PollForDecisionTaskResponse response = _swfClient.PollForDecisionTask(request);
            return response.DecisionTask;
        }

        /// <summary>
        /// Looks at the events on the task to find all the completed activities. Using the list of completed activites it figures out
        ///  what thumbnail hasn't been created yet and create a decision to start an activity task for that image size.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        List<Decision> Decide(DecisionTask task)
        {
            this._console.WriteLine("Processing decision task ...");
            List<Decision> decisions = new List<Decision>();

            List<ActivityState> activityStates;
            WorkFlowExecutionInput startingInput;
            ProcessHistory(task, out startingInput, out activityStates);

            // Loop through all the diffrent image sizes the workflow will create and
            // when we find one missing create that activity task for the missing image.
            //
            // To keep the sample simple each activity is scheduled one at a time. For better performance
            // the activities could be scheduled in parallel and then use the decider to check if all the activities 
            // have been completed.
            for (int size = 256; size >= 16; size /= 2)
            {
                if (activityStates.FirstOrDefault(x => x.ImageSize == size) == null)
                {
                    decisions.Add(CreateActivityDecision(startingInput, size));
                    break;
                }
            }

            // If there were decisions that means all the thumbnails have been created so we decided the workflow execution is complete.
            if (decisions.Count == 0)
            {
                this._console.WriteLine("Workflow execution complete for " + startingInput.SourceImageKey);
                decisions.Add(CreateCompleteWorkflowExecutionDecision(activityStates));
            }

            return decisions;
        }

        /// <summary>
        /// Process the history of events to find all completed activity events and the start event. Using that we can find out
        /// what image is being resized and what images still need to be completed.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="startingInput"></param>
        /// <param name="activityStates"></param>
        void ProcessHistory(DecisionTask task, out WorkFlowExecutionInput startingInput, out List<ActivityState> activityStates)
        {
            startingInput = null;
            activityStates = new List<ActivityState>();

            HistoryIterator iterator = new HistoryIterator(this._swfClient, task);
            foreach (var evnt in iterator)
            {
                if (evnt.EventType == EventType.WorkflowExecutionStarted)
                {
                    startingInput = Utils.DeserializeFromJSON<WorkFlowExecutionInput>(evnt.WorkflowExecutionStartedEventAttributes.Input);
                }
                if (evnt.EventType == EventType.ActivityTaskCompleted)
                {
                    ActivityState state = Utils.DeserializeFromJSON<ActivityState>(evnt.ActivityTaskCompletedEventAttributes.Result);
                    activityStates.Add(state);
                }
            }

        }

        /// <summary>
        /// The method tells SWF what decisions have been made for the current decision task.
        /// </summary>
        /// <param name="taskToken"></param>
        /// <param name="decisions"></param>
        void CompleteTask(string taskToken, List<Decision> decisions)
        {
            RespondDecisionTaskCompletedRequest request = new RespondDecisionTaskCompletedRequest()
            {
                Decisions = decisions,
                TaskToken = taskToken
            };

            this._swfClient.RespondDecisionTaskCompleted(request);
        }

        /// <summary>
        /// Helper method to create a decision for scheduling an activity
        /// </summary>
        /// <returns>Decision with ScheduleActivityTaskDecisionAttributes</returns>
        Decision CreateActivityDecision(WorkFlowExecutionInput startingInput, int imageSize)
        {
            // setup the input for the activity task.
            ActivityState state = new ActivityState
            {
                StartingInput = startingInput,
                ImageSize = imageSize
            };

            Decision decision = new Decision()
            {
                DecisionType = DecisionType.ScheduleActivityTask,
                ScheduleActivityTaskDecisionAttributes = new ScheduleActivityTaskDecisionAttributes()
                {
                    ActivityType = new ActivityType()
                    {
                        Name = Constants.ImageProcessingActivityName,
                        Version = Constants.ImageProcessingActivityVersion
                    },
                    ActivityId = Constants.ActivityIdPrefix + DateTime.Now.TimeOfDay,
                    Input = Utils.SerializeToJSON<ActivityState>(state)
                }
            };
            this._console.WriteLine(string.Format("Decision: Schedule Activity Task (Resize {0} to size {1})", state.StartingInput.SourceImageKey, imageSize));
            return decision;
        }

        /// <summary>
        /// Helper method to create a decision for completed workflow exeution. This happens once all the thumbnails have been created.
        /// </summary>
        /// <returns>Decision with ScheduleActivityTaskDecisionAttributes</returns>
        Decision CreateCompleteWorkflowExecutionDecision(List<ActivityState> states)
        {
            // Create a string listing all the images create.
            StringBuilder sb = new StringBuilder();
            states.ForEach(x => sb.AppendFormat("\tSize: {0} S3 Loc {1}: \r\n", x.ImageSize, x.ResizedImageKey));

            Decision decision = new Decision()
            {
                DecisionType = DecisionType.CompleteWorkflowExecution,
                CompleteWorkflowExecutionDecisionAttributes = new CompleteWorkflowExecutionDecisionAttributes
                {
                    Result = sb.ToString()
                }
            };
            this._console.WriteLine("Decision: Complete Workflow Execution");
            this._console.WriteLine(sb.ToString());
            return decision;
        }
    }
}
