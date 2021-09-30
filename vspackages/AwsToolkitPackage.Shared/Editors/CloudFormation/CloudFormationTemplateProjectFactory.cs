using System;
using System.Runtime.InteropServices;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio.Project;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.Editors.CloudFormation
{
    [Guid(GuidList.guidCloudFormationTemplateProjectFactoryString)]
    class CloudFormationTemplateProjectFactory : ProjectFactory
    {
        ILog LOGGER = LogManager.GetLogger(typeof(CloudFormationTemplateProjectFactory));
        private AWSToolkitPackage package;

        public CloudFormationTemplateProjectFactory(AWSToolkitPackage package)
            : base(package)
        {
            this.package = package;
        }

        protected override ProjectNode CreateProject()
        {
            LOGGER.Debug("Creating CloudFormation Project");
            CloudFormationTemplateProjectNode project = new CloudFormationTemplateProjectNode(this.package);

            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                project.SetSite((IOleServiceProvider)((IServiceProvider)this.package).GetService(typeof(IOleServiceProvider)));
            });
            
            return project;
        }
    }
}
