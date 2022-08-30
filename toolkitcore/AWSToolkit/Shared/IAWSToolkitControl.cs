using System.ComponentModel;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.Shared
{
    public interface IAWSToolkitControl : INotifyPropertyChanged
    {
        string Title { get; }
        string UniqueId { get; }
        string MetricId { get; }

        /// <summary>
        /// Whether or not this control can be shown on
        /// a per UniqueId-account-region basis (true) or
        /// only per UniqueId (false)
        /// </summary>
        bool IsUniquePerAccountAndRegion { get; }

        UserControl UserControl { get; }

        bool SupportsBackGroundDataLoad { get; }
        void ExecuteBackGroundLoadDataLoad();

        bool Validated();
        bool OnCommit();

        void RefreshInitialData(object initialData);
        object GetInitialData();

        bool SupportsDynamicOKEnablement { get; }

        void OnEditorOpened(bool success);

        bool CanClose();

        string AcceptButtonText { get; }

        string RejectButtonText { get; }

        /// <summary>
        /// Returns null if the dialog doesn't have an associated Help action.
        /// </summary>
        IHelpHandler GetHelpHandler();
    }
}
