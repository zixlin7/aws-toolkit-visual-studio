using System.Collections.Generic;
using System.Reflection;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public class ProfilePropertyMapping
    {
        private static readonly PropertyInfo[] CredentialProfileReflectionProperties =
            typeof(CredentialProfileOptions).GetProperties();

        private readonly Dictionary<string, string> _nameMapping;

        public ProfilePropertyMapping(Dictionary<string, string> nameMapping)
        {
            this._nameMapping = nameMapping;
        }

        /// <summary>
        /// Separate the profileDictionary into its profileOptions
        /// </summary>
        /// <param name="profileDictionary">Dictionary with everything in it</param>
        /// <returns>The resulting CredentialProfileOptions</returns>returns>
        public CredentialProfileOptions ExtractProfileOptions(Dictionary<string, string> profileDictionary)
        {
            CredentialProfileOptions profileOptions = new CredentialProfileOptions();
            foreach (var reflectionProperty in CredentialProfileReflectionProperties)
            {
                if(_nameMapping.TryGetValue(reflectionProperty.Name, out var mappedName))
                {
                    if (mappedName != null && profileDictionary.TryGetValue(mappedName, out var value))
                    {
                        reflectionProperty.SetValue(profileOptions, value, null);
                    }
                }
            }

            return profileOptions;
        }
    }
}
