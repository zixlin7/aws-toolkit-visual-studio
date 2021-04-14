using System;
using System.Collections.Generic;
using System.Windows;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.AWSToolkit.DynamoDB.Util;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.DynamoDB.Nodes
{
    public class DynamoDBRootViewModel : ServiceRootViewModel, IDynamoDBRootViewModel
    {
        private readonly DynamoDBRootViewMetaNode _metaNode;
        private readonly IRegionProvider _regionProvider;
        private readonly Lazy<IAmazonDynamoDB> _ddbClient;

        public DynamoDBRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region, IRegionProvider regionProvider)
            : base(accountViewModel.MetaNode.FindChild < DynamoDBRootViewMetaNode>(), accountViewModel, "Amazon DynamoDB", region)
        {
            _metaNode = base.MetaNode as DynamoDBRootViewMetaNode;
            _regionProvider = regionProvider;
            _ddbClient = new Lazy<IAmazonDynamoDB>(CreateDynamoDbClient);

            DynamoDBLocalManager.Instance.StartedJavaProcessExited += new EventHandler(DyanmoDBLocalProcessExited);
        }

        void DyanmoDBLocalProcessExited(object sender, EventArgs e)
        {
            if (IsLocalRegion())
            {
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() => UpdateDynamoDBLocalState());
            }
        }

        public override string ToolTip => "Amazon DynamoDB is a fast, highly scalable, highly available, cost-effective, non-relational database service.";

        protected override string IconName => "Amazon.AWSToolkit.DynamoDB.Resources.EmbeddedImages.table.png";

        public override string Name
        {
            get
            {
                var name = base.Name;
                if (IsLocalRegion())
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
            this.Refresh(true);
            base.NotifyPropertyChanged(nameof(Name));
        }

        public override IList<ActionHandlerWrapper> GetVisibleActions()
        {
            var dynamoDBMetaNode = this.MetaNode as DynamoDBRootViewMetaNode;
            bool isLocal = IsLocalRegion();
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

        public IAmazonDynamoDB DynamoDBClient => this._ddbClient.Value;

        protected override void LoadChildren()
        {
            bool isLocal = IsLocalRegion();
            if (isLocal && DynamoDBLocalManager.Instance.State == DynamoDBLocalManager.CurrentState.Stopped)
            {
                SetChildren(new List<IViewModel>());
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
                SetChildren(items);
            }
            catch (Exception e)
            {
                if (isLocal)
                {
                    SetChildren(new List<IViewModel>{new LocalConnectFailViewModel(this, DynamoDBLocalManager.Instance.LastConfiguredPort)});
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

        private bool IsLocalRegion()
        {
            return _regionProvider.IsRegionLocal(Region.Id);
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            try
            {
                dndDataObjects.SetData("ARN", string.Format("arn:aws:ddb:{0}:{1}:*",
                    this.Region.Id, ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId));
            }
            catch (Exception)
            {
                // Eat the error, don't destabilize the call stack
                // Don't spam the log - this event can happen frequently
            }
        }

        private IAmazonDynamoDB CreateDynamoDbClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonDynamoDBClient>(Region);
        }
    }
}
