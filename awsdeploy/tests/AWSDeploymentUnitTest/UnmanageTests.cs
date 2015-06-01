using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceProcess;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

using AWSDeploymentHostManager;
using AWSDeploymentHostManager.Tasks;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class UnmanageTests
    {
        [TestMethod]
        [Ignore]
        public void ShutdownTest()
        {
            var controller = new ServiceController();
            controller.ServiceName = UnmanageTask.SERVICE_NAME;
            if (controller.Status != ServiceControllerStatus.Running)
                controller.Start();
            if (UnmanageTask.GetMagicHarpStartMethod() == UnmanageTask.SERVICE_DISABLE_START_STATUS)
                UnmanageTask.SetMagicHarpStartMethod(UnmanageTask.SERVICE_MANUAL_START_STATUS);

            UnmanageTask task = new UnmanageTask(false);
            task.DoExecute(null);

            Assert.AreEqual(ServiceControllerStatus.Stopped, controller.Status);
            Assert.AreEqual(UnmanageTask.SERVICE_DISABLE_START_STATUS, UnmanageTask.GetMagicHarpStartMethod());
        }
    }
}
