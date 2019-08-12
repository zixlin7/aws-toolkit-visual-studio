using System;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Navigator
{
    public class CommandInstantiator<T> where T : IContextCommand, new()
    {
        public ActionResults Execute(IViewModel viewModel)
        {
            try
            {
                ActionResults results = null;
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                    {
                        T command = new T();
                        results = command.Execute(viewModel);
                    }));
                return results;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Unknown Error: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }
    }
}
