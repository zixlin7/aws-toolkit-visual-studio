using System;
using System.Collections.Generic;
using System.Threading;

namespace Amazon.AWSToolkit.CommonUI.JobTracker
{
    public abstract class QueueProcessingJob : BaseJob
    {
        readonly object QUEUE_ACECSS_LOCK = new object();
        readonly object WAIT_FOR_COMPLETION_LOCK = new object();

        int _totalNumberOfUnits;
        Queue<IJobUnit> _pendingUnits;
        Queue<IJobUnit> _completedUnits;
        Thread[] _executedThreads;
        Invoker[] _invokers;

        protected abstract Queue<IJobUnit> BuildQueueOfJobUnits();

        protected abstract string CurrentStatusPostFix
        {
            get;
        }

        protected virtual int NumberActiveThreads => 5;

        protected override void ExecuteJob()
        {
            this._pendingUnits = BuildQueueOfJobUnits();
            this._totalNumberOfUnits = this._pendingUnits.Count;
            this._completedUnits = new Queue<IJobUnit>(this._totalNumberOfUnits);

            this.ProgressMin = 0;
            this.ProgressValue = 0;
            this.ProgressMax = this._totalNumberOfUnits;
            Exception exception = null;
            try
            {
                this.startInvokerPool();
                this.waitTillAllThreadsComplete();
            }
            catch (Exception e)
            {
                exception = e;
                this.shutdown();
                throw;
            }
            finally
            {
                PostExecuteJob(exception);
            }
        }

        protected virtual void PostExecuteJob(Exception exception)
        {
        }

        private void startInvokerPool()
        {
            int threadCount = this.NumberActiveThreads;
            if (threadCount > this._totalNumberOfUnits)
            {
                threadCount = this._totalNumberOfUnits;
            }

            this._executedThreads = new Thread[threadCount];
            this._invokers = new Invoker[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                this._invokers[i] = new Invoker(this);
                Thread thread = new Thread(new ThreadStart(this._invokers[i].Execute));
                thread.IsBackground = true;
                this._executedThreads[i] = thread;
                thread.Start();
            }
        }

        private void waitTillAllThreadsComplete()
        {
            lock (this.WAIT_FOR_COMPLETION_LOCK)
            {
                while (this._completedUnits.Count != this._totalNumberOfUnits)
                {
                    Monitor.Wait(this.WAIT_FOR_COMPLETION_LOCK, 100);

                    // Look for any exceptions from the upload threads.
                    foreach (Invoker invoker in this._invokers)
                    {
                        checkForLastException(invoker);
                    }

                    this.ProgressValue = this._completedUnits.Count;
                    this.CurrentStatus = string.Format("{0} / {1} {2}", this.CompletedUnits, this.TotalUnits, this.CurrentStatusPostFix);
                }

                this.ProgressValue = this._completedUnits.Count;
                this.CurrentStatus = string.Format("{0} / {1} {2}", this.CompletedUnits, this.TotalUnits, this.CurrentStatusPostFix);
            }
        }

        protected virtual int CompletedUnits => this._completedUnits.Count;

        protected virtual int TotalUnits => this._totalNumberOfUnits;

        private void shutdown()
        {
            bool anyAlive = true;
            for (int i = 0; anyAlive && i < 5; i++)
            {
                anyAlive = false;
                foreach (Thread thread in this._executedThreads)
                {
                    try
                    {
                        if (thread.IsAlive)
                        {
                            thread.Abort();
                            anyAlive = true;
                        }
                    }
                    catch { }
                }
            }
        }

        private void checkForLastException(Invoker invoker)
        {
            if (invoker.LastException != null)
                throw invoker.LastException;
        }

        public interface IJobUnit
        {
            void Execute();
        }

        class Invoker
        {
            QueueProcessingJob _job;
            Exception _lastException;

            internal Invoker(QueueProcessingJob job)
            {
                this._job = job;
            }

            internal Exception LastException => this._lastException;

            private IJobUnit getNextJobUnit()
            {
                lock (this._job.QUEUE_ACECSS_LOCK)
                {
                    if (this._job._pendingUnits.Count == 0)
                    {
                        return null;
                    }
                    return this._job._pendingUnits.Dequeue();
                }
            }

            internal void Execute()
            {
                IJobUnit jobUnit;
                while ((jobUnit = getNextJobUnit()) != null)
                {
                    this._lastException = null;
                    try
                    {
                        jobUnit.Execute();
                        lock (this._job.WAIT_FOR_COMPLETION_LOCK)
                        {
                            this._job._completedUnits.Enqueue(jobUnit);
                            Monitor.Pulse(this._job.WAIT_FOR_COMPLETION_LOCK);
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        this._lastException = e;
                        lock (this._job.WAIT_FOR_COMPLETION_LOCK)
                        {
                            Monitor.Pulse(this._job.WAIT_FOR_COMPLETION_LOCK);
                        }
                        break;
                    }
                }
            }
        }
    }
}
