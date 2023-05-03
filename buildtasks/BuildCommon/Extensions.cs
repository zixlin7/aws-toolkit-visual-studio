using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ThirdParty.Json.LitJson;

namespace BuildCommon
{
    public static class Extensions
    {
        public static JsonData SafeGet(this JsonData data, string property)
        {
            if (data == null)
                return null;

            JsonData subData = null;
            try
            {
                subData = data[property];
            }
            catch (KeyNotFoundException)
            { }

            return subData;
        }

        public static TValue SafeGet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = new TValue();
                dictionary[key] = value;
            }
            return value;
        }

        public enum VersionElement
        {
            Major = 0,
            Minor = 1,
            Build = 2,
            Revision = 3
        }
        public static Version Increment(this Version self, VersionElement element, int delta = 1)
        {
            return Increment(self, (int)element, delta);
        }
        public static Version Increment(this Version self, int position, int delta)
        {
            switch(position)
            {
                case 0:
                    return new Version(self.Major + delta, 0, 0, 0);
                case 1:
                    return new Version(self.Major, self.Minor + delta, 0, 0);
                case 2:
                    return new Version(self.Major, self.Minor, self.Build + delta, 0);
                case 3:
                    return new Version(self.Major, self.Minor, self.Build, self.Revision + delta);
                default:
                    throw new ArgumentOutOfRangeException("position");
            }
        }

        public static Version GetMajorMinorVersion(this Version self)
        {
            return new Version(self.Major, self.Minor);
        }

        public static VersionElement? GreatestCommonElement(this Version self, Version other)
        {
            if (self.Major != other.Major)
                return VersionElement.Major;
            if (self.Minor != other.Minor)
                return VersionElement.Minor;
            if (self.Build != other.Build)
                return VersionElement.Build;
            if (self.Revision != other.Revision)
                return VersionElement.Revision;

            // versions are identical
            return null;
        }
    }
}
