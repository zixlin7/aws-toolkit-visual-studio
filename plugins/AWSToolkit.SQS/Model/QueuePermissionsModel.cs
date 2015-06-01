using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.Auth.AccessControlPolicy;

namespace Amazon.AWSToolkit.SQS.Model
{
    public class QueuePermissionsModel
    {
        Dictionary<string, PermissionRecord> _original = new Dictionary<string, PermissionRecord>();
        ObservableCollection<PermissionRecord> _permissions = new ObservableCollection<PermissionRecord>();

        public QueuePermissionsModel()
        {
        }

        public string[] Actions
        {
            get
            {
                return new string[] { "*", "SendMessage", "ReceiveMessage", "DeleteMessage", "ChangeMessageVisibility", "GetQueueAttributes" };
            }
        }

        public QueuePermissionsModel(string policy)
        {
            parse(policy);
        }

        public ObservableCollection<PermissionRecord> Permissions
        {
            get
            {
                return this._permissions;
            }
        }

        private void parse(string policyStr)
        {
            if (string.IsNullOrEmpty(policyStr))
                return;

            Policy policy = Policy.FromJson(policyStr);

            foreach (var statement in policy.Statements)
            {
                if (string.IsNullOrEmpty(statement.Id))
                    continue;
                string label = statement.Id;
                label = stripQuotes(label);

                if (statement.Actions.Count == 0)
                    continue;

                string action = null;
                foreach (ActionIdentifier item in statement.Actions)
                {
                    if (statement.Conditions.Count > 0)
                        continue;

                    if (item.ActionName.ToUpper().StartsWith("SQS:"))
                    {
                        action = item.ActionName.Substring(4);
                        action = stripQuotes(action);

                        string awsAccountId = null;
                        foreach (var principal in statement.Principals)
                        {
                            if ("AWS".Equals(principal.Provider))
                            {
                                awsAccountId = principal.Id;
                                awsAccountId = stripQuotes(awsAccountId);

                                this._original.Add(label, new PermissionRecord(label, awsAccountId, action));
                                this._permissions.Add(new PermissionRecord(label, awsAccountId, action));
                            }
                        }
                    }
                }
            }
        }

        private string stripQuotes(string token)
        {
            if (string.IsNullOrEmpty(token))
                return token;

            if(token.StartsWith("\""))
                token = token.Substring(1);
            if(token.EndsWith("\""))
                token = token.Substring(0, token.Length - 1);

            return token;
        }

        /// <summary>
        /// Returns back a list of actions that need to be taken in order to persist the permissions.
        /// </summary>
        /// <returns></returns>
        public List<PersistAction> GetPersistActions()
        {
            List<PersistAction> actions = new List<PersistAction>();
            HashSet<string> labelsForCurrent = new HashSet<string>();

            foreach (PermissionRecord record in this._permissions)
            {
                PermissionRecord originalRecord;
                if (this._original.TryGetValue(record.Label, out originalRecord))
                {
                    if (!record.AWSAccountId.Equals(originalRecord.AWSAccountId) ||
                        !record.Action.Equals(originalRecord.Action))
                    {
                        actions.Add(new PersistAction(PersistAction.Action.Modify, record));
                    }
                }
                else
                {
                    actions.Add(new PersistAction(PersistAction.Action.Add, record));
                }

                labelsForCurrent.Add(record.Label);
            }

            foreach (PermissionRecord record in this._original.Values)
            {
                if (!labelsForCurrent.Contains(record.Label))
                {
                    actions.Add(new PersistAction(PersistAction.Action.Delete, record));
                }
            }
            

            return actions;
        }

        public void CommitEdits()
        {
            this._original.Clear();
            foreach (PermissionRecord record in this._permissions)
            {
                this._original.Add(record.Label, new PermissionRecord(record.Label, record.AWSAccountId, record.Action));
            }
        }

        public class PersistAction
        {
            public enum Action { Add, Modify, Delete };

            public PersistAction(PersistAction.Action action, PermissionRecord record)
            {
                this.ActionToTake = action;
                this.Record = record;
            }

            public PermissionRecord Record
            {
                get;
                set;
            }

            public PersistAction.Action ActionToTake
            {
                get;
                set;
            }
        }

        public class PermissionRecord
        {

            public PermissionRecord()
            {
                this.Label = Guid.NewGuid().ToString().Replace("-", "");
                this.Action = "*";
            }

            public PermissionRecord(string label, string awsAccountId, string action)
            {
                this.Label = label;
                this.AWSAccountId = awsAccountId;
                this.Action = action;
            }

            public string Label
            {
                get;
                set;
            }

            public string AWSAccountId
            {
                get;
                set;
            }

            public string Action
            {
                get;
                set;
            }
        }
    }
}
