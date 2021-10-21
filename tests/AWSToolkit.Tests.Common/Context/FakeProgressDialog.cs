using System.Collections.Generic;
using System.Threading;

using Amazon.AWSToolkit.CommonUI.Notifications.Progress;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    public class FakeProgressDialog : IProgressDialog
    {
        private string _heading1;

        public bool CancelRequested { get; set; }

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public string Caption { get; set; }

        public string Heading1
        {
            get => _heading1;
            set
            {
                _heading1 = value;
                Heading1History.Add(value);
            }
        }

        public IList<string> Heading1History = new List<string>();

        public string Heading2 { get; set; }

        public bool CanCancel { get; set; }

        public int CurrentStep { get; set; }

        public int TotalSteps { get; set; }

        public void Show()
        {
        }

        public void Show(int secondsDelay)
        {
        }

        public void Hide()
        {
        }

        public bool IsCancelRequested() => CancelRequested;

        public void Dispose()
        {
        }
    }
}
