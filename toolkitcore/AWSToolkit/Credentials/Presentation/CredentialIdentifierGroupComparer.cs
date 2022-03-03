using System;
using System.Collections;
using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.Credentials.Presentation
{
    /// <summary>
    /// Provides group based comparisons of credential Ids.
    /// The ids are grouped based on their factories.
    /// </summary>
    public class CredentialIdentifierGroupComparer : IComparer<ICredentialIdentifier>, IComparer<CredentialsIdentifierGroup>, IComparer
    {
        public int Compare(object x, object y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }
            if (ReferenceEquals(null, y))
            {
                return 1;
            }
            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            if (x is CredentialsIdentifierGroup || y is CredentialsIdentifierGroup)
            {
                return Compare(x as CredentialsIdentifierGroup, y as CredentialsIdentifierGroup);
            }

            if (x is ICredentialIdentifier || y is ICredentialIdentifier)
            {
                return Compare(x as ICredentialIdentifier, y as ICredentialIdentifier);
            }

            return 0;
        }

        public int Compare(CredentialsIdentifierGroup x, CredentialsIdentifierGroup y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }
            if (ReferenceEquals(null, y))
            {
                return 1;
            }
            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            var sortPriorityComparison = x.SortPriority.CompareTo(y.SortPriority);
            if (sortPriorityComparison != 0)
                return sortPriorityComparison;
            return string.Compare(x.GroupName, y.GroupName, StringComparison.Ordinal);
        }

        public int Compare(ICredentialIdentifier x, ICredentialIdentifier y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }
            if (ReferenceEquals(null, y))
            {
                return 1;
            }
            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            var groupComparison = Compare(x.GetPresentationGroup(), y.GetPresentationGroup());
            if (groupComparison != 0)
            {
                return groupComparison;
            }

            return string.Compare(x.DisplayName, y.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
