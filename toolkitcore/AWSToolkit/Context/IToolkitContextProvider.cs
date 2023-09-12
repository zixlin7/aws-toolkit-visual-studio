using System;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.Context
{
    /// <summary>
    /// Allows us to dependency inject the ToolkitContext into systems.
    /// This is primarily intended for use with MEF components, which
    /// can be activated independently from the main AWS Toolkit package.
    /// </summary>
    public interface IToolkitContextProvider
    {
        /// <summary>
        /// Indicates if the provider has been initialized with a ToolkitContext
        /// </summary>
        /// <returns></returns>
        bool HasToolkitContext();

        /// <summary>
        /// Gets the ToolkitContext object.
        /// Throws if ToolkitContext is not available.
        /// </summary>
        /// <returns></returns>
        ToolkitContext GetToolkitContext();

        /// <summary>
        /// Waits until ToolkitContext is available, then returns it.
        /// This should not be called on UI threads.
        /// </summary>
        Task<ToolkitContext> WaitForToolkitContextAsync();

        /// <summary>
        /// Waits until ToolkitContext is available, then returns it.
        /// Overload: Throws an exception if ToolkitContext is not available by the specified time.
        /// This should not be called on UI threads, to avoid a deadlock on the UI thread.
        /// </summary>
        /// <param name="timeoutMs">The number of milliseconds to wait. An exception is thrown if ToolkitContext is not available within specified time.</param>
        Task<ToolkitContext> WaitForToolkitContextAsync(int timeoutMs);

        /// <summary>
        /// Registers a callback to be invoked when this object has been initialized with a ToolkitContext.
        /// If this object has already been initialized, the callback is immediately called.
        /// </summary>
        void RegisterOnInitializedCallback(Action callback);
    }
}
