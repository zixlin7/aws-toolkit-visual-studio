using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Publish.Package;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Core PublishToAws VS Package components that most of the functionality should have access to.
    /// The intent of this class is to pass core components around without a lengthy
    /// parameter list.
    ///
    /// The contents of this class are intended to be mock-able.
    /// </summary>
    public class PublishContext
    {
        public IPublishToAwsPackage PublishPackage { get; set; }
        public ToolkitContext ToolkitContext { get; set; }
        public IAWSToolkitShellProvider ToolkitShellProvider { get; set; }
        public InstallOptions InstallOptions { get; set; }
        public Task InitializeCliTask { get; set; }

        public IPublishSettingsRepository PublishSettingsRepository { get; set; }
    }
}
