using System.Collections.Generic;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework
{
    /// <summary>
    /// Factory for construction of various types of wizard environments
    /// </summary>
    public static class AWSWizardFactory
    {
        /// <summary>
        /// Constructs a 'standard layout' dialog-type wizard with the specified ID.
        /// </summary>
        /// <param name="wizardID">
        /// Can be used to identify the wizard for diagnostic purposes or during events
        /// propagated from the wizard
        /// </param>
        /// <param name="initialProperties">
        /// Initial properties to seed into the wizard; may be null
        /// </param>
        /// <returns>Interface to the wizard instance</returns>
        public static IAWSWizard CreateStandardWizard(string wizardID, IDictionary<string, object> initialProperties)
        {
            return new AWSStandardWizard(new AWSBaseWizardImpl(wizardID, initialProperties));
        }
    }
}
