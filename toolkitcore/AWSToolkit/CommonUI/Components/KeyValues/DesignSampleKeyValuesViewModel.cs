using Amazon.AWSToolkit.Models;
using Amazon.AWSToolkit.ViewModels;

namespace Amazon.AWSToolkit.CommonUI.Components.KeyValues
{
    /// <summary>
    /// Populates design time views with sample data
    /// </summary>
    public class DesignSampleKeyValuesViewModel : KeyValuesViewModel
    {
        public DesignSampleKeyValuesViewModel()
        {
            Add("PATH", "/app");
            Add("hello", "world");
            Add("hello", "world");
        }

        private void Add(string key, string value)
        {
            KeyValues.Add(new KeyValue(key, value));
        }
    }
}
