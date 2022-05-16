using System;

using BuildTasks.VersionManagement;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTasks.Test
{
    [TestClass]
    public class CreateTestVersionTaskTests
    {
        private CreateTestVersionTask _sut;

        [TestInitialize]
        public void TestSetup()
        {
            _sut = new CreateTestVersionTask();
        }

        [TestMethod]
        public void GetVersion()
        {
            var now = DateTime.Now;
            _sut.InitialVersion = "1.2.3.4";
            Assert.IsTrue(_sut.Execute(now));
            Assert.AreEqual($"1.2.3.{GetDateDecoration(now)}4", _sut.Version);
        }

        private string GetDateDecoration(DateTime dateTime)
        {
            // eg: October 1 -> "1001"
            var month = dateTime.Month;
            var day = dateTime.Day;
            return $"{month}{day:00}";
        }
    }
}
