using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AwsToolkit.VsSdk.Common.Commands
{
    public static class KeyBindingUtilities
    {
        /// <summary>
        /// Removes the scope portion from the key binding
        /// </summary>
        /// <example>
        /// Input: "Global::Alt+C"
        /// Returns: "Alt+C"
        /// </example>
        public static string FormatKeyBindingDisplayText(string binding)
        {
            if (string.IsNullOrEmpty(binding))
            {
                return string.Empty;
            }

            var lastIndex = binding.LastIndexOf("::", StringComparison.Ordinal);

            if (lastIndex >= 0)
            {
                binding = binding.Substring(lastIndex + 2);
            }

            return binding;
        }
    }
}
