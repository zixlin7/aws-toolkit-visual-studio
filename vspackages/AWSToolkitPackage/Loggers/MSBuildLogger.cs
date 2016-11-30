using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Amazon.AWSToolkit.VisualStudio.Loggers
{
    // This logger will derive from the Microsoft.Build.Utilities.Logger class,
    // which provides it with getters and setters for Verbosity and Parameters,
    // and a default empty Shutdown() implementation.
    internal class MSBuildLogger : Logger
    {
        readonly IBuildAndDeploymentLogger _outerLogger;
        readonly string _msgPrefix = string.Empty;

        internal MSBuildLogger(IBuildAndDeploymentLogger outerLogger, string msgPrefix)
        {
            this._outerLogger = outerLogger;
            if (!string.IsNullOrEmpty(msgPrefix))
            {
                if (msgPrefix.EndsWith(" "))
                    _msgPrefix = msgPrefix;
                else
                    _msgPrefix = msgPrefix + " ";
            }
        }

        /// <summary>
        /// Initialize is guaranteed to be called by MSBuild at the start of the build
        /// before any events are raised.
        /// </summary>
        public override void Initialize(IEventSource eventSource)
        {
            if (eventSource == null)
                return;

            // msbuild can generate a lot of detailed events, with many overlapping,
            // so just select the ones we need for sensible display in our output
            // window tab
            //eventSource.AnyEventRaised += eventSource_AnyEventRaised;
            eventSource.ProjectStarted += eventSource_ProjectStarted;
            //eventSource.TaskStarted += eventSource_TaskStarted;
            //eventSource.MessageRaised += eventSource_MessageRaised;
            eventSource.WarningRaised += eventSource_WarningRaised;
            eventSource.ErrorRaised += eventSource_ErrorRaised;
            eventSource.ProjectFinished += eventSource_ProjectFinished;
            //eventSource.BuildStarted += eventSource_BuildStarted;
            //eventSource.BuildFinished += eventSource_BuildFinished;
            //eventSource.TargetFinished += eventSource_TargetFinished;
            //eventSource.TargetStarted += eventSource_TargetStarted;
        }

        //void eventSource_TargetStarted(object sender, TargetStartedEventArgs e)
        //{
        //}

        //void eventSource_TargetFinished(object sender, TargetFinishedEventArgs e)
        //{
        //}

        //void eventSource_BuildStarted(object sender, BuildStartedEventArgs e)
        //{
        //}

        //void eventSource_BuildFinished(object sender, BuildFinishedEventArgs e)
        //{
        //}

        //void eventSource_AnyEventRaised(object sender, BuildEventArgs e)
        //{
        //}

        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            var line = String.Format("build error: '{0}' at ({1},{2}): {3}", e.File, e.LineNumber, e.ColumnNumber, e.Message);
            WriteLineWithSenderAndMessage(line, e, true);
        }

        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            var line = String.Format("build warning: '{0}' at ({1},{2}): {3}", e.File, e.LineNumber, e.ColumnNumber, e.Message);
            WriteLineWithSenderAndMessage(line, e, true);
        }

        //void eventSource_MessageRaised(object sender, BuildMessageEventArgs e)
        //{
        //    // this yields a very detailed trace, so only use for diagnostic purposes
        //    if ((e.Importance == MessageImportance.High && IsVerbosityAtLeast(LoggerVerbosity.Minimal))
        //        || (e.Importance == MessageImportance.Normal && IsVerbosityAtLeast(LoggerVerbosity.Normal))
        //        || (e.Importance == MessageImportance.Low && IsVerbosityAtLeast(LoggerVerbosity.Detailed))
        //        )
        //    {
        //        WriteLineWithSenderAndMessage("eventSource_MessageRaised: ", e, true);
        //    }
        //}

        //void eventSource_TaskStarted(object sender, TaskStartedEventArgs e)
        //{
        //}

        void eventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            string msg;
            if (!string.IsNullOrEmpty(e.TargetNames))
            {
                msg = string.Format("executing target(s) \"{0}\"", string.Join(";", e.TargetNames));
            }
            else
            {
                msg = string.Format("executing default target");
            }

            WriteLineWithSenderAndMessage(msg, e, true);
        }

        void eventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            var msg = string.Format("project build completed {0}.", e.Succeeded ? "successfully" : "with errors");
            WriteLineWithSenderAndMessage(msg, e, true);
        }

        /// <summary>
        /// Write a line to the log, adding the SenderName and Message
        /// (these parameters are on all MSBuild event argument objects)
        /// </summary>
        private void WriteLineWithSenderAndMessage(string line, BuildEventArgs e, bool forceVisible)
        {
            WriteLine("MSBuild".Equals(e.SenderName, StringComparison.OrdinalIgnoreCase) 
                ? string.Format("{0} {1}", _msgPrefix, line) 
                : string.Format("{0} {1}: {2}", _msgPrefix, e.SenderName, line), e, forceVisible);
        }

        /// <summary>
        /// Just write a line to the log
        /// </summary>
        private void WriteLine(string line, BuildEventArgs e, bool forceVisible)
        {
            if (_outerLogger == null)
                return;

            _outerLogger.OutputMessage(line, true, forceVisible);
        }

        /// <summary>
        /// Shutdown() is guaranteed to be called by MSBuild at the end of the build, after all 
        /// events have been raised.
        /// </summary>
        public override void Shutdown()
        {
        }
    }
}