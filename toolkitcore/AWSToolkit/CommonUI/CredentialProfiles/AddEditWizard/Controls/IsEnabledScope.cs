using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Controls
{
    /// <summary>
    /// Breaks the IsEnabled inheritance tree to override the IsEnabled setting.
    /// </summary>
    /// <remarks>
    /// This control should only be used in cases where the control of IsEnabled outside
    /// of the normal and expected inherited behavior is desirable.  For example, if an
    /// entire form should be disabled, but scrollbars should remain enabled to view lists
    /// or a cancel button should remain enabled while all other controls are disabled.
    ///
    /// As the scope is broken for all descendants, care must be taken if some subtree(s)
    /// of this control should retain the inherited IsEnabled behavior.  Binding the parent
    /// of those subtree(s) IsEnabled property to a control or DataContext higher in the
    /// visual tree than this control would be one solution.
    /// </remarks>
    public class IsEnabledScope : ContentControl
    {
        static IsEnabledScope()
        {
            IsEnabledProperty.OverrideMetadata(
                typeof(IsEnabledScope),
                new UIPropertyMetadata(
                    defaultValue: true,
                    propertyChangedCallback: (sender, e) => { },
                    coerceValueCallback: (d, baseValue) => baseValue));
        }
    }
}
