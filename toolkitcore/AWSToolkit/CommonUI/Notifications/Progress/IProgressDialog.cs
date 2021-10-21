using System;
using System.Threading;

namespace Amazon.AWSToolkit.CommonUI.Notifications.Progress
{
    /// <summary>
    /// Abstraction from VS SDK around a Progress notification dialog.
    /// This is intended for use with longer running processes, so that the user can see that
    /// the Toolkit is doing something, and (optionally) have a chance to cancel it.
    ///
    /// To use:
    /// - Set Caption, TotalSteps, CanCancel before displaying (at a minimum)
    /// - Call Show prior to performing processing
    /// - Frequently check CancellationToken/IsCancelRequested, and update Heading1, Heading2, and CurrentStep, CanCancel as needed
    /// - Call Hide after processing completes or is cancelled
    /// - Dispose
    /// </summary>
    public interface IProgressDialog : IDisposable
    {
        /// <summary>
        /// Represents if a user has pressed the Cancel button on the dialog
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// The dialog's caption
        /// </summary>
        string Caption { get; set; }

        /// <summary>
        /// The dialog's primary message label
        /// </summary>
        string Heading1 { get; set; }

        /// <summary>
        /// The dialog's secondary message label
        /// </summary>
        string Heading2 { get; set; }

        /// <summary>
        /// Whether or not users are allowed to cancel the dialog while processing is taking place
        /// </summary>
        bool CanCancel { get; set; }

        /// <summary>
        /// Visually represents how far along the process is
        /// </summary>
        int CurrentStep { get; set; }

        /// <summary>
        /// Visually represents how far along the process is
        /// </summary>
        int TotalSteps { get; set; }

        /// <summary>
        /// Immediately shows the progress dialog
        /// </summary>
        void Show();

        /// <summary>
        /// Shows the progress dialog after <see cref="secondsDelay"/> seconds, unless it has been cancelled by then.
        /// </summary>
        void Show(int secondsDelay);

        /// <summary>
        /// Hides the progress dialog
        /// </summary>
        void Hide();

        /// <summary>
        /// Queries whether nor not the user has pressed Cancel
        /// </summary>
        bool IsCancelRequested();
    }
}