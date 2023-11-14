using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    /// <summary>
    /// Handles logging reference tracking (suggestions that contain license attribution)
    /// </summary>
    public interface IReferenceLogger
    {
        /// <summary>
        /// Displays the log of licenses attributed to accepted suggestions
        /// </summary>
        Task ShowAsync();

        /// <summary>
        /// Logs a license attribution associated with an accepted suggestion
        /// </summary>
        Task LogReferenceAsync(LogReferenceRequest request);
    }
}
