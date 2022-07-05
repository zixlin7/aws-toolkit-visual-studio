using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Amazon.AWSToolkit.Publish.PublishSetting
{
    /// <summary>
    /// Represents all publish experience related settings
    /// </summary>
    public class PublishSettings : IEquatable<PublishSettings>
    {
        public DeployServerSettings DeployServer { get; set; }

        public List<string> SilencedPublishConfirmations { get; set; } = new List<string>();

        public static PublishSettings CreateDefault()
        {
            return new PublishSettings()
            {
                SilencedPublishConfirmations = new List<string>(),
                DeployServer = DeployServerSettings.CreateDefault()
            };
        }

        [OnDeserialized]
        internal void SetDefaultValuesAfterDeserialization(StreamingContext context)
        {
            DeployServer = DeployServer ?? DeployServerSettings.CreateDefault();
            SilencedPublishConfirmations = SilencedPublishConfirmations ?? new List<string>();
        }


        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (DeployServer != null ? DeployServer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SilencedPublishConfirmations.GetHashCode();
                return hashCode;
            }
        }
        public bool Equals(PublishSettings other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Equals(DeployServer, other.DeployServer) &&
                   SilencedPublishConfirmations.SequenceEqual(other.SilencedPublishConfirmations);
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
