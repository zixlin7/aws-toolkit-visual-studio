using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public abstract class BulkChangeController<C,T> : BaseContextCommand where T : IWrapper
    {
        const int MAX_ROWS_TO_SHOW = 10;

        public override ActionResults Execute(IViewModel model)
        {
            return new ActionResults().WithSuccess(false);
        }

        public ActionResults Execute(C client, IList<T> wrappers)
        {
            string msg = buildConfirmMessage(wrappers);
            if (!ToolkitFactory.Instance.ShellProvider.Confirm(this.Action, string.Format(msg)))
            {
                return new ActionResults()
                    .WithCancelled(true)
                    .WithSuccess(false);
            }

            Dictionary<T, Exception> failures = new Dictionary<T, Exception>();
            foreach (var instance in wrappers)
            {
                try
                {
                    PerformAction(client, instance);
                }
                catch (Exception e)
                {
                    failures[instance] = e;
                }
            }

            if (failures.Count > 0)
            {
                string failedMsg = buildFailureErrorMessage(failures);
                ToolkitFactory.Instance.ShellProvider.ShowError(failedMsg);

                if (wrappers.Count == failures.Count)
                    return new ActionResults().WithSuccess(false);
            }

            return new ActionResults().WithSuccess(true);
        }

        private string buildConfirmMessage(IList<T> wrappers)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.ConfirmMessage);
            sb.AppendLine();

            int rows = 0;
            foreach (var instance in wrappers)
            {
                sb.AppendLine(instance.DisplayName);
                rows++;
                if (rows >= MAX_ROWS_TO_SHOW)
                {
                    sb.AppendLine("...");
                    break;
                }
            }

            return sb.ToString();
        }

        private string buildFailureErrorMessage(Dictionary<T, Exception> failures)
        {
            if (failures.Count() < 1)
                return ("Success!");  //TODO: This should be found by the code review to be unsatisfactory.

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Failed to {0} for the following {1}(s):\n", this.Action.ToLower(), failures.First().Key.TypeName);

            int rows = 0;
            foreach (var kvp in failures)
            {
                sb.AppendFormat("{0}: {1}\n", kvp.Key.DisplayName, kvp.Value.Message);
                rows++;
                if (rows >= MAX_ROWS_TO_SHOW)
                {
                    sb.AppendLine("...");
                    break;
                }
            }

            return sb.ToString();
        }

        protected abstract string Action { get; }

        protected abstract string ConfirmMessage { get; }

        protected abstract void PerformAction(C client, T instance);
    }
}
