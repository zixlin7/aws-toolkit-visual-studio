using System;
using System.Threading;
using System.Threading.Tasks;

using log4net;

namespace Amazon.AWSToolkit.Events
{
    /// <summary>
    /// Provides convenience wrapper methods to convert Event-based asynchronous patterns to Task-based.
    /// </summary>
    /// <remarks>
    /// This class implements a lambda-based approach to wrapping the code rather than a class extension
    /// approach.  This helps reduce the number of limited-use classes added to the code base as well
    /// as supports the unique ability of lambdas to "capture" variables/references in the defining context
    /// which is often needed to support this pattern.  It's much easier to reference private methods in
    /// this way than to define a child class that has access to the parent classes non-public members.
    /// <see cref="https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/">
    /// Asynchronous programming patterns</see> for more details on these asynchronous patterns.
    /// </remarks>
    public static class EventWrapperTask
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(EventWrapperTask));

        /// <summary>
        /// Wraps common generics EventHandler&lt;TEventArgs&gt; with event-specific delegate when needed for
        /// legacy events.
        /// </summary>
        /// <typeparam name="TLegacyEventHandler">The legacy event handler delegate to return.</typeparam>
        /// <typeparam name="TEventArgs">The event args that both the handler passed and returned must support.</typeparam>
        /// <param name="handler">The EventHandler&lt;TEventArgs&gt; to be wrapped.</param>
        /// <returns>The handler converted to the legacy event handler.</returns>
        /// <remarks>
        /// The EventHandler&lt;TEventArgs&gt; and legacy event handler represented by TEventDelegate must both
        /// have the same signature.  This conversion cannot be supported at compile-time due to limitations in
        /// the C# generics specification, delegates have very limited support.  It is important that unit-tests
        /// for consuming code exercise calls to ToEventDelegate either directly or indirectly as correctness can
        /// only be evaluated at run-time.
        ///
        /// See EventWrapperTaskTests.DoesNotTerminateUntilSetResultCalledAsync for an example.
        /// </remarks>
        public static TLegacyEventHandler ToLegacyEventHandler<TLegacyEventHandler, TEventArgs>(EventHandler<TEventArgs> handler)
            where TEventArgs : EventArgs
            where TLegacyEventHandler : Delegate
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var legacyParams = typeof(TLegacyEventHandler).GetMethod("Invoke").GetParameters();

            if (!(legacyParams.Length == 2
                && legacyParams[0].ParameterType == typeof(object)
                && legacyParams[1].ParameterType.IsAssignableFrom(typeof(TEventArgs))))
            {
                var msg = $"EventHandler<{typeof(TEventArgs)}> signature must be equivalent to {typeof(TLegacyEventHandler)}";
                _logger.Error(msg);
                throw new InvalidCastException(msg);
            }

            return (TLegacyEventHandler) Delegate.CreateDelegate(typeof(TLegacyEventHandler), handler.Target, handler.Method);
        }

        /// <summary>
        /// Provides a convenience wrapper to implement the pattern to convert an Event-based asynchronous patterns
        /// to a Task-based one.
        /// </summary>
        /// <typeparam name="TEventArgs">The event args used by the event to be wrapped.</typeparam>
        /// <typeparam name="TResult">The result of the Task.  For void Tasks, use object with setResult(null).</typeparam>
        /// <param name="addHandler">Adds the passed handler to the event.  See Remarks section if handler and event
        /// have different delegate types.</param>
        /// <param name="start">The code to start the process that will raise the event.  This is typically the
        /// synchronous method being wrapped.</param>
        /// <param name="handleEvent">Handles the event.  The third parameter setResult allows setting the Task result and
        /// returning the awaited Task.  See Remarks for more details.</param>
        /// <param name="removeHandler">Removes the passed handler from the event.  See Remarks section if ToEventDelegate used.</param>
        /// <param name="cancellationToken">CancellationToken, if provided, to cancel running task.</param>
        /// <returns>The Task representing the wrapped event.</returns>
        /// <remarks>
        /// The parameters of this method form a strategy to implement the pattern to convert an Event-based asynchronous pattern
        /// to a Task-based one.
        ///
        /// Adding and removing handlers is straight-forward for EventHandler&lt;TEventArgs&gt; style events.  Just reference the
        /// event (lambda captures are useful here) and use += and -= to wire/unwire the handler.  If the event uses a legacy
        /// handler delegate (i.e. a custom delegate with the pattern sender/e params signature) use the ToLegacyEventHandler method
        /// to wrap the passed handler in the expected delegate type for the event.  As the removeHandler will need the reference
        /// to the delegate used in AddHandler, you should assign a captured variable of TLegacyEventHandler in addHandler that
        /// can be referenced in removeHandler.  See EventWrapperTaskTests.DoesNotTerminateUntilSetResultCalledAsync for an example.
        ///
        /// The "start" delegate can be whatever setup code is needed to start the process that will raise event(s).  This will typically
        /// be just calling the synchronous version of the method that is being made async, but can be any code that is needed.
        ///
        /// The "handleEvent" delegate is called each time the event is raised with the args passed to the event handler.  Additionally,
        /// a setResult delegate is passed that can be used to set the Task result and terminate event handling.  This means that the
        /// Task can terminate on the first call to the event or allows for multiple calls to the event.  The handler will continue to
        /// be called until either setResult is called or an exception is thrown.  The logic of handleEvent determines when to return
        /// from the awaited Task.  Exceptions are automatically captured and set on the Task if they occur.  While CancellationTokens
        /// aren't supported in the signature of this method, they can easily be added and used as captured variables.
        ///
        /// If a timeout or other reason to cancel the event handler is required.  Use a CancellationTokenSource to create a CancellationToken
        /// with the desired timeout.  This will remove the event handler and set canceled on the task.  This will throw a TaskCanceledEvent
        /// that should be caught and handled by the caller.  Do not use Task.Wait with a timeout as this only instructs the task to stop
        /// waiting and return from await, but it does not terminate the task from running.  The event handler will continue to be called.
        /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation">Task cancellation</see>
        /// for more details.
        /// 
        /// See CredentialSettingsManager.CreateProfileAsync and AwsConnectionManager.ChangeConnectionSettingsAsync for
        /// examples.
        /// </remarks>
        public static Task<TResult> Create<TEventArgs, TResult>(
            Action<EventHandler<TEventArgs>> addHandler,
            Action start,
            Action<object, TEventArgs, Action<TResult>> handleEvent,
            Action<EventHandler<TEventArgs>> removeHandler,
            CancellationToken cancellationToken = default)
            where TEventArgs : EventArgs
        {
            var taskSource = new TaskCompletionSource<TResult>();
            Action<TResult> setResult = null;
            EventHandler<TEventArgs> handler = null;

            void Finish(TaskStatus setStatus, object parameter = null)
            {
                removeHandler(handler);
                switch (setStatus)
                {
                    case TaskStatus.Canceled:
                        if (!taskSource.TrySetCanceled())
                        {
                            _logger.Warn($"Unable to set canceled on {nameof(EventWrapperTask)}.");
                        }
                        break;
                    case TaskStatus.Faulted:
                        if (!taskSource.TrySetException((Exception) parameter))
                        {
                            _logger.Warn($"Unable to set exception on {nameof(EventWrapperTask)}.");
                        }
                        break;
                    case TaskStatus.RanToCompletion:
                        if (!taskSource.TrySetResult((TResult) parameter))
                        {
                            _logger.Warn($"Unable to set result on {nameof(EventWrapperTask)}.");
                        }
                        break;
                }
            }

            handler = (object sender, TEventArgs e) =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    handleEvent(sender, e, setResult);
                }
                catch (OperationCanceledException)
                {
                    Finish(TaskStatus.Canceled);
                }
                catch (Exception ex)
                {
                    Finish(TaskStatus.Faulted, ex);
                }
            };

            setResult = result => Finish(TaskStatus.RanToCompletion, result);

            try
            {
                addHandler(handler);

                // Register the callback to finish with cancelled immediately after
                // adding the handler above.  If the CancellationToken is already
                // canceled at this point, Register will run the callback synchronously
                // right now.  We want to be sure the handler is removed from the
                // event in the case that this operation is cancelled before it even 
                // gets started. Otherwise, the callback will be run when cancellation
                // has been requested on the token (for example, due to timeout).
                cancellationToken.Register(() => Finish(TaskStatus.Canceled));

                if (!taskSource.Task.IsCanceled)
                {
                    start();
                }
            }
            catch (Exception ex)
            {
                Finish(TaskStatus.Faulted, ex);
            }

            return taskSource.Task;
        }
    }
}
