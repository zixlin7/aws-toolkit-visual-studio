using System;
using System.Threading;

using Amazon.AWSToolkit.CommonUI.Notifications.Progress;
using Amazon.AWSToolkit.Shared;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AwsToolkit.VsSdk.Common.Notifications
{
    /// <summary>
    /// Implementation of a wrapper around Visual Studio's Progress notification dialog.
    /// This is intended for use with longer running processes, so that the user can see that
    /// the Toolkit is doing something, and (optionally) have a chance to cancel it.
    /// </summary>
    public class ProgressDialog : IProgressDialog, IVsThreadedWaitDialogCallback
    {
        private readonly IVsThreadedWaitDialog4 _waitDialog;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        private bool _isShowing = false;

        public string Caption { get; set; }

        private string _heading1;
        public string Heading1
        {
            get => _heading1;
            set
            {
                _heading1 = value;
                Update();
            }
        }

        private string _heading2;
        public string Heading2
        {
            get => _heading2;
            set
            {
                _heading2 = value;
                Update();
            }
        }

        private bool _canCancel = true;
        public bool CanCancel
        {
            get => _canCancel;
            set
            {
                _canCancel = value;
                Update();
            }
        }

        private int _currentStep;
        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                _currentStep = value;
                Update();
            }
        }

        private int _totalSteps;
        public int TotalSteps
        {
            get => _totalSteps;
            set
            {
                _totalSteps = value;
                Update();
            }
        }

        public ProgressDialog(IVsThreadedWaitDialogFactory waitDialogFactory)
        {
            _waitDialog = waitDialogFactory.CreateInstance();
        }

        public void Show() => Show(0);

        public void Show(int secondsDelay)
        {
            _waitDialog.StartWaitDialogWithCallback(
                Caption,
                Heading1,
                Heading2,
                null,
                null,
                CanCancel,
                secondsDelay,
                true,
                TotalSteps,
                CurrentStep,
                this
                );
            _isShowing = true;
        }

        public void Hide()
        {
            _waitDialog.EndWaitDialog(out _);
            _isShowing = false;
        }

        public bool IsCancelRequested()
        {
            _waitDialog.HasCanceled(out bool cancelRequested);
            return cancelRequested;
        }

        private void Update()
        {
            if (_isShowing)
            {
                _waitDialog.UpdateProgress(
                    Heading1,
                    Heading2,
                    null,
                    CurrentStep,
                    TotalSteps,
                    !CanCancel,
                    out bool cancelRequested);

                if (cancelRequested)
                {
                    OnCanceled();
                }
            }
        }

        public void OnCanceled()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public void Dispose()
        {
            if (_isShowing)
            {
                Hide();
            }

            _cancellationTokenSource?.Dispose();
        }
    }
}
