using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Amazon.AWSToolkit.CommonUI.Notifications
{
    /// <summary>
    /// Background watcher onto a task that presents a popup toaster
    /// when that task signals it is complete.
    /// </summary>
    public class TaskWatcher
    {
        // by default we'll call the completion proxy every 5 seconds
        public static readonly TimeSpan DefaultPollInterval = new TimeSpan(0, 0, 5);

        /// <summary>
        /// The active state of the watcher
        /// </summary>
        public enum WatcherState
        {
            pending = 0,
            watching,
            cancelling,
            cancelled,
            timedOut,
            completedError,
            completedOK
        }

        /// <summary>
        /// The current state of the watched task as reported by 
        /// IQueryTaskCompletionProxy.QueryTaskCompletion
        /// </summary>
        public enum TaskCompletionState { pending, error, completed };

        /// <summary>
        /// Interface onto a proxy object passed to TaskNotifier instances
        /// that can check the underlying work for completion. Note that this
        /// is called from a worker thread.
        /// </summary>
        public interface IQueryTaskCompletionProxy
        {
            TaskCompletionState QueryTaskCompletion(TaskWatcher callingNotifier);
        }

        /// <summary>
        /// Interface onto a proxy object that can render whatever UI is needed
        /// to inform the user that a long-running task is done.
        /// </summary>
        public interface INotifyTaskCompletionProxy
        {
            void NotifyTaskCompletion(TaskWatcher callingNotifier);
        }

        /// <summary>
        /// Custom state data that can be accessed from this instance in the callback
        /// proxies.
        /// </summary>
        public object PrivateState { get; set; }

        /// <summary>
        /// Instantiate and run a watcher using the specified polling interval and object
        /// implementing query/complete callback interfaces.
        /// </summary>
        /// <param name="pollInterval"></param>
        /// <param name="queryAndNotificationProxy"></param>
        /// <returns></returns>
        public static TaskWatcher WatchAndNotify(TimeSpan pollInterval,
                                                 object queryAndNotificationProxy)
        {
            TaskWatcher tn = new TaskWatcher(pollInterval, 
                                                queryAndNotificationProxy as IQueryTaskCompletionProxy,
                                                queryAndNotificationProxy as INotifyTaskCompletionProxy);
            tn.Timeout = TimeSpan.Zero;
            tn.Run();

            return tn;
        }

        /// <summary>
        /// Instantiate and run a watcher using the specified polling interval and callback to
        /// determine completion
        /// </summary>
        /// <param name="pollInterval"></param>
        /// <param name="queryCompletionProxy"></param>
        /// <param name="notifyCompletionProxy"></param>
        /// <returns></returns>
        public static TaskWatcher WatchAndNotify(TimeSpan pollInterval, 
                                                 IQueryTaskCompletionProxy queryCompletionProxy,
                                                 INotifyTaskCompletionProxy notifyCompletionProxy)
        {
            TaskWatcher tn = new TaskWatcher(pollInterval, queryCompletionProxy, notifyCompletionProxy);
            tn.Timeout = TimeSpan.Zero;
            tn.Run();

            return tn;
        }

        /// <summary>
        /// Instantiate and run a watcher using the specified polling interval and callback to
        /// determine completion. The watcher will be abandoned after the specified timeout interval
        /// elapses.
        /// </summary>
        /// <param name="pollInterval"></param>
        /// <param name="completionProxy"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TaskWatcher WatchAndNotify(TimeSpan pollInterval, 
                                                 TimeSpan timeout, 
                                                 IQueryTaskCompletionProxy queryCompletionProxy,
                                                 INotifyTaskCompletionProxy notifyCompletionProxy)
        {
            TaskWatcher tn = new TaskWatcher(pollInterval, queryCompletionProxy, notifyCompletionProxy);
            tn.Timeout = timeout;
            tn.Run();
            
            return tn;
        }

        /// <summary>
        /// Instantiates a watcher instance with a given polling interval and completion callbacks;
        /// the watcher does not start until Run is called
        /// </summary>
        private TaskWatcher(TimeSpan pollInterval, 
                             IQueryTaskCompletionProxy queryCompletionProxy,
                             INotifyTaskCompletionProxy notifyCompletionProxy)
        {
            this.PollInterval = pollInterval;
            this.QueryTaskCompletionProxy = queryCompletionProxy;
            this.NotifyTaskCompletionProxy = notifyCompletionProxy;
        }

        /// <summary>
        /// Instantiate a watcher with the default poll interval. A completion callback
        /// can be specified separately. 
        /// </summary>
        public TaskWatcher()
        {
            this.PollInterval = DefaultPollInterval;
        }

        /// <summary>
        /// Sets the watcher thread to be abandoned at the next polling opportunity
        /// </summary>
        public void Cancel()
        {
            lock (_syncLock)
            {
                this._runState = WatcherState.cancelling;
            }
        }

        /// <summary>
        /// Indicates the executing state of the notifier
        /// </summary>
        public WatcherState WatchingState
        {
            get
            {
                WatcherState rs;
                lock (_syncLock)
                {
                    rs = this._runState;
                }

                return rs;
            }

            protected set
            {
                this._runState = value;
            }
        }

        public IQueryTaskCompletionProxy QueryTaskCompletionProxy { get; set; }

        public INotifyTaskCompletionProxy NotifyTaskCompletionProxy { get; set; }

        /// <summary>
        /// Polling interval for completion
        /// </summary>
        public TimeSpan PollInterval { get; set; }

        /// <summary>
        /// Sets an elapsed time after which the watcher will be abandoned.
        /// </summary>
        /// <remarks>
        /// By default, TimeSpan.Zero which means the watcher runs until
        /// the task completes or forever, whichever occurs first!
        /// </remarks>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Sets the specified completion callback and executes the watcher
        /// </summary>
        /// <param name="completionProy"></param>
        public void Run(IQueryTaskCompletionProxy completionProy)
        {
            this.QueryTaskCompletionProxy = completionProy;
            Run();
        }

        /// <summary>
        /// Executes the watcher
        /// </summary>
        /// <remarks>A completion callback must have been set before this method is called</remarks>
        public void Run()
        {
            if (this.QueryTaskCompletionProxy == null)
                throw new InvalidOperationException("No completion callback has been set");

            ThreadPool.QueueUserWorkItem(new WaitCallback(PollingWorker), this);
        }

        /// <summary>
        /// The background polling method
        /// </summary>
        /// <param name="stateObj">Instance of TaskNotifier</param>
        void PollingWorker(object stateObj)
        {
            TaskWatcher thisNotifier = stateObj as TaskWatcher;
            thisNotifier.WatchingState = WatcherState.watching;

            DateTime? endAfter = null;
            if (thisNotifier.Timeout != TimeSpan.Zero)
                endAfter = DateTime.UtcNow + thisNotifier.Timeout;

            bool quit = false;
            do
            {
                Thread.Sleep(thisNotifier.PollInterval);

                if (thisNotifier.WatchingState != WatcherState.cancelling)
                {
                    TaskCompletionState taskState = thisNotifier.QueryTaskCompletionProxy.QueryTaskCompletion(thisNotifier);
                    if (taskState != TaskCompletionState.pending)
                    {
                        thisNotifier.WatchingState = taskState == TaskCompletionState.completed ? WatcherState.completedOK : WatcherState.completedError;
                        thisNotifier.NotifyTaskCompletionProxy.NotifyTaskCompletion(thisNotifier);
                        
                        quit = true;
                    }
                }
                else
                {
                    quit = true;
                    thisNotifier.WatchingState = WatcherState.cancelled;
                }

                if (endAfter != null)
                {
                    if (DateTime.UtcNow.CompareTo(endAfter) > 0)
                    {
                        quit = true;
                        thisNotifier.WatchingState = WatcherState.timedOut;
                    }
                }

            } while (!quit);
        }

        object _syncLock = new object();
        WatcherState _runState = WatcherState.pending;
    }
}
