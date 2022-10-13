using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.Commands
{
    public class ShowMessageInOutputCommand
    {
        private ShowMessageInOutputCommand() { }

        public static readonly ILog Logger = LogManager.GetLogger(typeof(ShowMessageInOutputCommand));

        public static ICommand Create(IAWSToolkitShellProvider toolkitHost)
        {
            return new RelayCommand(obj => ShowMessage(obj, toolkitHost));
        }

        private static void ShowMessage(object obj, IAWSToolkitShellProvider toolkitHost)
        {
            try
            {
                var message = (string) obj;
                toolkitHost.OutputToHostConsole(message, true);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show message in output window", ex);
            }
        }
    }
}
