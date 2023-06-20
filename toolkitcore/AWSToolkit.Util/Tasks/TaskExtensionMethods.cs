using System;
using System.Threading.Tasks;
using log4net;

namespace Amazon.AWSToolkit.Tasks
{
    public static class TaskExtensionMethods
    {
        internal static ILog LOGGER = LogManager.GetLogger(typeof(TaskExtensionMethods));

        /// <summary>
        /// Consumes a Task, ignores the result, and logs any Exceptions that arose in the task.
        /// Handy for calls that do not care about a Task's completion.
        /// Prevents unobserved Exceptions when firing and forgetting Tasks.
        /// </summary>
        /// <see cref="https://github.com/microsoft/vs-threading/blob/main/doc/cookbook_vs.md#how-to-write-a-fire-and-forget-method-responsibly"/>
        /// <param name="task">The task to consume.</param>
        public static void LogExceptionAndForget(this Task task)
        {
            if (!task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                // Silence "missing await" warning with Discard operator
                _ = AwaitAndLogException(task);
            }
        }

        private static async Task AwaitAndLogException(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                LOGGER.Error($"Encountered Exception in Task", e);
            }
        }
    }
}
