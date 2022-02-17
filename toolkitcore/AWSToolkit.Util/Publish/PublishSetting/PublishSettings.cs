using System;
using System.Runtime.Serialization;

namespace Amazon.AWSToolkit.Publish.PublishSetting
{
    /// <summary>
    /// Represents all publish experience related settings
    /// </summary>
    public class PublishSettings : IEquatable<PublishSettings>
    {
        public DeployServerSettings DeployServer { get; set; }

        public bool ShowPublishBanner { get; set; } = true;

        public static PublishSettings CreateDefault()
        {
            return new PublishSettings()
            {
                ShowPublishBanner = true,
                DeployServer = DeployServerSettings.CreateDefault()
            };
        }

        [OnDeserialized]
        internal void SetDefaultValuesAfterDeserialization(StreamingContext context)
        {
            DeployServer = DeployServer ?? DeployServerSettings.CreateDefault();
        }


        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (DeployServer != null ? DeployServer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ShowPublishBanner.GetHashCode();
                return hashCode;
            }
        }
        public bool Equals(PublishSettings other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Equals(DeployServer, other.DeployServer) && ShowPublishBanner == other.ShowPublishBanner;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PublishSettings)obj);
        }

        public static bool operator ==(PublishSettings left, PublishSettings right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PublishSettings left, PublishSettings right)
        {
            return !Equals(left, right);
        }
    }
}
