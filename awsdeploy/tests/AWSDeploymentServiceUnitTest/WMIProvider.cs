using System;
using System.Collections;
using System.ComponentModel;
using System.Management.Instrumentation;

//Required for instance-level WMI information
[assembly: WmiConfiguration(@"root\Amazon", HostingModel = ManagementHostingModel.Decoupled)]
//Required for publishing the Event
[assembly: Instrumented(@"root\Amazon")]

namespace MagicHarpServiceUnitTest.WMIProvider
{
    [ManagementEntity(Name = "EC2_ConfigService")]
    public sealed class ConfigServiceInstance
    {
        private static ConfigServiceInstance m_Instance = new ConfigServiceInstance();
        private string m_InstanceID;

        private ConfigServiceInstance()
        {
            m_InstanceID = Guid.NewGuid().ToString("D");
        }

        private ConfigServiceInstance(string id)
        {
            m_InstanceID = id;
        }

        public static ConfigServiceInstance Instance
        {
            get { return m_Instance; }
        }

        public static void Publish()
        {
            InstrumentationManager.Publish(m_Instance);
        }

        public static void Unpublish()
        {
            InstrumentationManager.Revoke(m_Instance);
        }

        #region Public WMI Properties

        [ManagementKey]
        public string ID
        {
            get { return this.m_InstanceID; }
        }

        [ManagementProbe] //implies read-only
        public bool ConfigurationComplete
        {
            get { return m_ConfigurationComplete; }
            set { m_ConfigurationComplete = value; }
        }
        private bool m_ConfigurationComplete = false;

        [ManagementEnumerator]
        static public IEnumerable EnumerateConfigServiceInstances()
        {
            yield return Instance.m_InstanceID;
        }

        /// <summary>
        /// Supports getting instance by its ID.
        /// </summary>                
        public static ConfigServiceInstance GetInstance(string ID)
        {
            //
            //Only supporting the single instance currently, so just match the ID
            //and return null if it doesn't match.
            //
            if (ID.Equals(Instance.m_InstanceID))
                return Instance;
            else
                return null;
        }
        #endregion
    }

    /// <summary>
    /// Class used to instantiate WMI Provider Events.  Uses .NET 2.0 InstrumentationClass
    /// eventing.  
    /// </summary>
    [InstrumentationClass(InstrumentationType.Event)]
    public class EC2_ConfigServiceEvent
    {
        private EC2_ConfigServiceEvent(EC2_ConfigServiceEventType eventType, string id, string message)
        {
            m_EventType = eventType;
            EventMessage = message;
            Id = id;
        }

        private EC2_ConfigServiceEventType m_EventType;
        public int EventType
        {
            get { return (int)m_EventType; }
        }


        public string Id { get; private set; }
        public string EventMessage { get; private set; }


        public static void Publish(EC2_ConfigServiceEventType eventType, string id, string message)
        {

            Instrumentation.Fire(new EC2_ConfigServiceEvent(eventType, id, message));
        }
    }

    public enum EC2_ConfigServiceEventType
    {
        ConfigurationComplete = 0
    }

    [RunInstaller(true)]
    public class ManagementInstaller : System.Management.Instrumentation.DefaultManagementInstaller
    {
    }
}