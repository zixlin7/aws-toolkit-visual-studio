using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AWSDeploymentHostManager.Persistence
{
    public class EntityObject
    {
        public EntityObject(EntityType entityType)
        {
            this.EntityType = entityType;
            this.Parameters = new Dictionary<string, string>();
        }

        public Guid Id
        {
            get;
            set;
        }

        public EntityType EntityType
        {
            get;
            set;
        }

        public string Status
        {
            get;
            set;
        }

        public DateTime Timestamp
        {
            get;
            internal set;
        }

        public IDictionary<string, string> Parameters
        {
            get;
            set;
        }
    }
}
