using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio.Project;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.CloudFormationEditor
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

            project.SetSite((IOleServiceProvider)((IServiceProvider)this.package).GetService(typeof(IOleServiceProvider)));
            return project;
        }
    }
}
