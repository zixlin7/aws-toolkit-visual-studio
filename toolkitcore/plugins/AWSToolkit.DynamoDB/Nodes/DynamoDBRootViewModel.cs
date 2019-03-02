using System;
using System.Collections.Generic;
using System.Windows;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Amazon.AWSToolkit.DynamoDB.Controller;
using Amazon.AWSToolkit.DynamoDB.Util;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.DynamoDB.Nodes
{
    public class DynamoDBRootViewModel : ServiceRootViewModel, IDynamoDBRootViewModel
    {
        DynamoDBRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;
        IAmazonDynamoDB _ddbClient;

        public DynamoDBRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild < DynamoDBRootViewMetaNode>(), accountViewModel, "Amazon DynamoDB")
        {
            this._metaNode = base.MetaNode as DynamoDBRootViewMetaNode;
            this._accountViewModel = accountViewModel;

            DynamoDBLocalManager.Instance.StartedJavaProcessExited += new EventHandler(DyanmoDBLocalProcessExited);
        }

        void DyanmoDBLocalProcessExited(object sender, EventArgs e)
        {
            if (this.CurrentEndPoint.RegionSystemName == RegionEndPointsManager.GetInstance().LocalRegion.SystemName)
            {
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(
                    (Action)(() => UpdateDynamoDBLocalState()));
            }
        }

        public override string ToolTip
        {
            get
            {
                return "Amazon DynamoDB is a fast, highly scalable, highly available, cost-effective, non-relational database service.";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.DynamoDB.Resources.EmbeddedImages.table.png";
            }
        }

        public override string Name
        {
            get
            {
                var name = base.Name;
                bool isLocal = this.CurrentEndPoint.RegionSystemName == RegionEndPointsManager.GetInstance().LocalRegion.SystemName;
                if (isLocal)
                {
                    if (DynamoDBLocalManager.Instance.State == DynamoDBLocalManager.CurrentState.Started)
                        name += string.Format(" (Started at http://localhost:{0})", DynamoDBLocalManager.Instance.LastConfiguredPort);
                    else if (DynamoDBLocalManager.Instance.State == DynamoDBLocalManager.CurrentState.Connected)
                        name += string.Format(" (Connected at http://localhost:{0})", DynamoDBLocalManager.Instance.LastConfiguredPort);
                    else
                        name += " (Stopped)";
                }

                return name;
            }
        }

        public void UpdateDynamoDBLocalState()
        {
            this.UpdateEndPoint(RegionEndPointsManager.GetInstance().LocalRegion.SystemName);
            base.NotifyPropertyChanged("Name");
        }

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            AmazonDynamoDBConfig config = new AmazonDynamoDBConfig();
            this.CurrentEndPoint.ApplyToClientConfig(config);
            this._ddbClient = new AmazonDynamoDBClient(awsCredentials, config);            
        }

        public override IList<ActionHandlerWrapper> GetVisibleActions()
        {
            var dynamoDBMetaNode = this.MetaNode as DynamoDBRootViewMetaNode;
            bool isLocal = this.CurrentEndPoint.RegionSystemName == RegionEndPointsManager.GetInstance().LocalRegion.SystemName;
            bool isConnected = DynamoDBLocalManager.Instance.State == DynamoDBLocalManager.CurrentState.Connected || DynamoDBLocalManager.Instance.State == DynamoDBLocalManager.CurrentState.Started;
            IList<ActionHandlerWrapper> actions = new List<ActionHandlerWrapper>();
            foreach (var action in this.MetaNode.Actions)
            {
                // Null means add a separator so just add it and continue.
                if (action == null)
                {
                    actions.Add(action);
                    continue;
                }

                if ((action.Handler == dynamoDBMetaNode.OnStartLocal || action.Handler == dynamoDBMetaNode.OnStopLocal)
                    && !isLocal)
                    continue;

                if (action.Handler == dynamoDBMetaNode.OnStartLocal && DynamoDBLocalManager.Instance.IsRunning)
                    continue;

                if (action.Handler == dynamoDBMetaNode.OnStopLocal && !DynamoDBLocalManager.Instance.IsRunning)
                    continue;

                if (isLocal && action.Handler == dynamoDBMetaNode.OnTableCreate && !isConnected)
                    continue;

                actions.Add(action);
            }

            return actions;
        }

        public IAmazonDynamoDB DynamoDBClient
        {
            get
            {
                return this._ddbClient;
            }
        }

        protected override void LoadChildren()
        {
            bool isLocal = this.CurrentEndPoint.RegionSystemName == RegionEndPointsManager.GetInstance().LocalRegion.SystemName;
            if (isLocal && DynamoDBLocalManager.Instance.State == DynamoDBLocalManager.CurrentState.Stopped)
            {
                BeginCopingChildren(new List<IViewModel>());
                return;
            }

            try
            {
                var dynamoDBMetaNode = this.MetaNode as DynamoDBRootViewMetaNode;

                List<IViewModel> items = new List<IViewModel>();
                var request = new ListTablesRequest();
                ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

                var tableNames = new List<string>();
                ListTablesResponse response = new ListTablesResponse();
                do
                {
                    request.ExclusiveStartTableName = response.LastEvaluatedTableName;
                    response = this.DynamoDBClient.ListTables(request);
                    tableNames.AddRange(response.TableNames);

                } while (!string.IsNullOrEmpty(response.LastEvaluatedTableName));

                foreach (var tableName in tableNames)
                {
                    var child = new DynamoDBTableViewModel(this._metaNode.DynamoDBTableViewMetaNode, this, tableName);
                    items.Add(child);
                }

                items.Sort(new Comparison<IViewModel>(AWSViewModel.CompareViewModel));
                BeginCopingChildren(items);
            }
            catch (Exception e)
            {
                if (isLocal)
                {
                    BeginCopingChildren(new List<IViewModel>{new LocalConnectFailViewModel(this, DynamoDBLocalManager.Instance.LastConfiguredPort)});
                }
                else
                {
                    AddErrorChild(e);
                }
            }
        }

        internal void AddTable(string tableName)
        {
            DynamoDBTableViewModel viewModel = new DynamoDBTableViewModel(this._metaNode.DynamoDBTableViewMetaNode, this, tableName);
            AddChild(viewModel);
        }

        internal void RemoveTable(string tableName)
        {
            this.RemoveChild(tableName);
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData("ARN", string.Format("arn:aws:ddb:{0}:{1}:*",
                this.CurrentEndPoint.RegionSystemName, this.AccountViewModel.AccountNumber, this.Name));
        }
}
}
