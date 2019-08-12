using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSInstanceViewMetaNode : RDSFeatureViewMetaNode
    {
        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View", OnView, null, true,
                    this.GetType().Assembly, null),
                null,
                new ActionHandlerWrapper("Add to Server Explorer...", OnAddToServerExplorer, null, false,
                    this.GetType().Assembly, null),
                new ActionHandlerWrapper
                {
                    Name = "Create SQL Server Database...",
                    Handler = OnCreateSQLServerDatabase,
                    IconAssembly = this.GetType().Assembly,
                    VisibilityHandler = SetCreateSQLServerDatabaseVisibility
                },
                null,
                new ActionHandlerWrapper("Modify Instance...", OnModify, null, false,
                    this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.ModifyDBInstance.png"),
                new ActionHandlerWrapper("Take Snapshot...", OnTakeSnapshot, null, false,
                    this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.TakeSnapshot.png"),
                new ActionHandlerWrapper("Reboot", OnReboot, null, false,
                    this.GetType().Assembly, null),
                null,
                new ActionHandlerWrapper("Delete Instance", OnDelete, null, false,
                    null, "delete.png")
            );

        public ActionHandlerWrapper.ActionHandler OnReboot
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnModify
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnTakeSnapshot
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnAddToServerExplorer
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnCreateSQLServerDatabase
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionVisibility SetCreateSQLServerDatabaseVisibility(IViewModel focus)
        {
            RDSInstanceViewModel instance = focus as RDSInstanceViewModel;
            if (instance != null && instance.DBInstance.DatabaseType != DatabaseTypes.SQLServer)
                return ActionHandlerWrapper.ActionVisibility.hidden;

            return ActionHandlerWrapper.ActionVisibility.enabled;
        }
    }
}
