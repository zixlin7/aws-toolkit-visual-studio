using Xunit;

namespace Amazon.AWSToolkit.Tests.Common.TestExtensions
{
    public sealed class Vs2019OrLaterFactAttribute : FactAttribute
    {
        public Vs2019OrLaterFactAttribute()
        {
            if (!IsVs2019OrLater())
            {
                Skip = "Only run on Visual Studio 2019 or later.";
            }
        }

        private bool IsVs2019OrLater()
        {
            #if VS2019_OR_LATER
                return true;
            #endif
                return false;
        }
    }
}
