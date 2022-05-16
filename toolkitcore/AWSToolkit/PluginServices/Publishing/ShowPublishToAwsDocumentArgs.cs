using System;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.PluginServices.Publishing
{
    public class ShowPublishToAwsDocumentArgs : IEquatable<ShowPublishToAwsDocumentArgs>
    {
        /// <summary>
        /// Display-friendly name of the project that will be published
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Full path of the project (eg .csproj file) that will be published
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// Unique identifier of the project (eg ProjectGuid of the .csproj file) that will be published
        /// </summary>
        public Guid ProjectGuid { get; set; }

        /// <summary>
        /// Initially selected Credentials to Publish with
        /// </summary>
        public ICredentialIdentifier CredentialId { get; set; }

        /// <summary>
        /// AccountId of initially selected Credentials to Publish with
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// Initially selected AWS Region to publish to
        /// </summary>
        public ToolkitRegion Region { get; set; }

        /// <summary>
        /// Where the request originated
        /// </summary>
        public string Requester { get; set; }

        public bool Equals(ShowPublishToAwsDocumentArgs other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ProjectName == other.ProjectName
                   && ProjectPath == other.ProjectPath
                   && Equals(ProjectGuid, other.ProjectGuid)
                   && Equals(CredentialId, other.CredentialId)
                   && Equals(Region, other.Region)
                   && Equals(Requester, other.Requester)
                   && Equals(AccountId, other.AccountId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ShowPublishToAwsDocumentArgs)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ProjectName != null ? ProjectName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProjectPath != null ? ProjectPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProjectGuid != null ? ProjectGuid.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CredentialId != null ? CredentialId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Region != null ? Region.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Requester != null ? Requester.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AccountId != null ? AccountId.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ShowPublishToAwsDocumentArgs left, ShowPublishToAwsDocumentArgs right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ShowPublishToAwsDocumentArgs left, ShowPublishToAwsDocumentArgs right)
        {
            return !Equals(left, right);
        }
    }
}
