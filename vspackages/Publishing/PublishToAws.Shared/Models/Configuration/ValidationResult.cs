using System.Collections.Generic;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    public class ValidationResult
    {
        private readonly Dictionary<string, string> _validationFailures = new Dictionary<string, string>();

        public void AddError(string detailId, string failureMessage)
        {
            _validationFailures.Add(detailId, failureMessage);
        }

        public bool HasError(string detailId)
        {
            return _validationFailures.ContainsKey(detailId);
        }

        public string GetError(string detailId)
        {
            return _validationFailures[detailId];
        }

        public IEnumerable<string> GetErrantDetailIds()
        {
            return _validationFailures.Keys;
        }

        public bool HasErrors()
        {
            return _validationFailures.Count > 0;
        }
    }
}
