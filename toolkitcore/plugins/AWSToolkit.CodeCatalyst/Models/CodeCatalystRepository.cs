using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.CodeCatalyst.Model;

using log4net;

namespace Amazon.AWSToolkit.CodeCatalyst.Models
{
    internal delegate Task<CloneUrls> CloneUrlsFactoryAsync(string repoName);

    internal class CloneUrls
    {
        internal CloneUrls(Uri https)
        {
            Https = https;
        }

        public Uri Https { get; }
    }

    internal class CodeCatalystRepository : ICodeCatalystRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeCatalystRepository));

        private readonly Lazy<Task<CloneUrls>> _cloneUrls;

        public string Name { get; }

        public string SpaceName { get; }

        public string ProjectName { get; }

        public string Description { get; }

        internal CodeCatalystRepository(CloneUrlsFactoryAsync cloneUrlsFactoryAsync, string name, string spaceName, string projectName, string description)
        {
            Arg.NotNull(name, nameof(name));
            Arg.NotNull(spaceName, nameof(spaceName));
            Arg.NotNull(projectName, nameof(projectName));

            _cloneUrls = new Lazy<Task<CloneUrls>>(() => cloneUrlsFactoryAsync(name), LazyThreadSafetyMode.ExecutionAndPublication);

            Name = name;
            SpaceName = spaceName;
            ProjectName = projectName;
            Description = description;
        }

        internal CodeCatalystRepository(CloneUrlsFactoryAsync cloneUrlsFactoryAsync, string spaceName, string projectName, ListSourceRepositoriesItem item)
            : this(cloneUrlsFactoryAsync, item.Name, spaceName, projectName, item.Description) { }

        protected bool Equals(CodeCatalystRepository other)
        {
            return Name == other.Name && SpaceName == other.SpaceName && ProjectName == other.ProjectName && Description == other.Description;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((CodeCatalystRepository) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SpaceName != null ? SpaceName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProjectName != null ? ProjectName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                return hashCode;
            }
        }

        public async Task<Uri> GetCloneUrlAsync(CloneUrlType cloneUrlType, ICodeCatalystAccessToken accessToken = null)
        {
            var cloneUrls = await _cloneUrls.Value;
            Uri url;

            switch (cloneUrlType)
            {
                case CloneUrlType.Https:
                    url = cloneUrls.Https;
                    break;
                default:
                    // This should never happen unless the enum is extended and a case statement is forgotten here
                    _logger.Error($"{cloneUrlType} is not supported.");
                    throw new ArgumentOutOfRangeException(nameof(cloneUrlType), cloneUrlType, "Not supported.");
            }

            if (!string.IsNullOrWhiteSpace(accessToken?.Secret))
            {
                url = new UriBuilder(url)
                {
                    // See https://github.com/dotnet/runtime/issues/74662 for why we Uri.EscapeDataString password
                    Password = Uri.EscapeDataString(accessToken.Secret)
                }.Uri;
            }

            return url;
        }
    }
}
