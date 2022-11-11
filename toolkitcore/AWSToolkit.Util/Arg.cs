using System;

namespace Amazon.AWSToolkit
{
    /// <summary>
    /// Argument validation methods.
    /// </summary>
    /// <remarks>
    /// Methods should be named such that they read naturally for comprehension.  For example, Arg.NotNull reads
    /// naturally as "Arg [is] not null".  "Is" has been dropped from the method names for brevity as it is redundant
    /// in all method names and does not impact comprehension.
    ///
    /// All methods defined here should throw ArgumentException or a derived type if the constraint defined in the
    /// method name is violated.
    /// </remarks>
    public static class Arg
    {
        public static void NotNull(object argValue, string paramName)
        {
            if (argValue == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void NotNullOrWhitespace(string argValue, string paramName)
        {
            if (string.IsNullOrWhiteSpace(argValue))
            {
                throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
            }
        }
    }
}
