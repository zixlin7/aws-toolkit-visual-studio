using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.JobTracker
{
    public abstract class BaseJob : BaseModel, IJob
    {
        public const string CANCELLED_STATUS = "Cancelled";
        public const string PENDING_STATUS = "Pending";

        bool _isComplete = false;

        long _progressMin = 0;
        long _progressMax = 1;
        long _progressValue = 0;
        string _progressToolTip;

        bool _isActionEnabled = true;

        string _title;
        string _currentStatus;

        Thread _executingThread;

        public BaseJob()
        {
            this.CurrentStatus = PENDING_STATUS;
        }

        public virtual Stream ActionIcon
        {
            get
            {
                if (!this.IsActionEnabled)
                    return null;

                Stream stream;
                if (this.IsComplete && this.CanResume)
                    stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.retry.png");
                else
                    stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.cancel.png");

                return stream;
            }
        }

        public virtual bool IsActionEnabled 
        {
            get { return this._isActionEnabled || this.CanResume; }
            set
            {
                this._isActionEnabled = value;
                base.NotifyPropertyChanged("IsActionEnabled");
            }
        }

        public virtual bool IsComplete
        {
            get
            {
                return this._isComplete;
            }
            set
            {
                this._isComplete = value;
                base.NotifyPropertyChanged("IsComplete");
            }
        }

        public virtual void Cancel()
        {
            this.IsActionEnabled = false;            
            try
            {
                if (this._executingThread != null)
                {
                    this._executingThread.Abort();
                }
                this.CurrentStatus = CANCELLED_STATUS;
            }
            catch { }
        }

        public void StartJob()
        {
            this._executingThread = new Thread(new ThreadStart(this.Execute));
            this._executingThread.IsBackground = true;
            this._executingThread.Start();
        }

        public void StartResume()
        {
            this._currentStatus = "";
            this.IsComplete = false;
            base.NotifyPropertyChanged("ActionIcon");

            this._executingThread = new Thread(new ThreadStart(this.Resume));
            this._executingThread.IsBackground = true;
            this._executingThread.Start();
        }

        public virtual string Title 
        {
            get
            {
                return this._title;
            }
            set
            {
                this._title = value;
                base.NotifyPropertyChanged("Title");
            }
        }

        public virtual string CurrentStatus
        {
            get
            {
                return this._currentStatus;
            }
            set
            {
                if (CANCELLED_STATUS.Equals(this.CurrentStatus))
                    return;

                this._currentStatus = value;
                base.NotifyPropertyChanged("CurrentStatus");
            }
        }


        public virtual long ProgressMin
        {
            get
            {
                return this._progressMin;
            }
            set
            {
                this._progressMin = value;
                base.NotifyPropertyChanged("ProgressMin");
            }
        }

        public virtual long ProgressMax
        {
            get
            {
                return this._progressMax;
            }
            set
            {
                this._progressMax = value;
                base.NotifyPropertyChanged("ProgressMax");
            }
        }

        public virtual long ProgressValue
        {
            get
            {
                return this._progressValue;
            }
            set
            {
                this._progressValue = value;
                base.NotifyPropertyChanged("ProgressValue");
            }
        }

        public virtual string ProgressToolTip
        {
            get
            {
                return this._progressToolTip;
            }
            set
            {
                this._progressToolTip = value;
                base.NotifyPropertyChanged("ProgressToolTip");
            }
        }

        public virtual bool CanResume
        {
            get { return false; }
        }

        public void Resume()
        {
            this.CurrentStatus = "";
            try
            {
                if (!this.CanResume)
                    return;

                this.ResumeJob();
            }
            catch (ThreadAbortException)
            {
                this.CurrentStatus = "Aborted";
            }
            catch (Exception e)
            {
                this.CurrentStatus = "Error: " + e.Message;
            }
            finally
            {
                this.IsComplete = true;
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    this.IsActionEnabled = false;
                    base.NotifyPropertyChanged("ActionIcon");
                }));
            }
        }

        public void Execute()
        {
            try
            {
                this.ExecuteJob();
            }
            catch (ThreadAbortException)
            {
                this.CurrentStatus = "Aborted";
            }
            catch (Exception e)
            {
                this.CurrentStatus = "Error: " + e.Message;
            }
            finally
            {
                this.IsComplete = true;
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    this.IsActionEnabled = false;
                    base.NotifyPropertyChanged("ActionIcon");
                }));
            }
        }

        protected abstract void ExecuteJob();

        protected virtual void ResumeJob()
        {
        }
    }
}
