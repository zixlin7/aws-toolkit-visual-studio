using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;

namespace Amazon.AWSToolkit.DynamoDB.Model
{
    public class TableBrowserModel : BaseModel
    {
        TableDescription _tableDescription;

        public TableBrowserModel(string tableName)
        {
            this.TableName = tableName;
            this.SecondaryIndexes = new ObservableCollection<KeySchemaExtendedElement>();
            this._documents.CollectionChanged += this.onDocumentsChanged;
        }

        public TableDescription TableDescription
        {
            get => this._tableDescription;
            set
            {
                this._tableDescription = value;
                SetHashAndRangeKeys();
                base.NotifyPropertyChanged("TableDescription");
                base.NotifyPropertyChanged("TableStatus");
                base.NotifyPropertyChanged("TableStatusColor");
                base.NotifyPropertyChanged("CanScan");
            }
        }        

        public string TableName
        {
            get;
        }

        public string TableStatus => this._tableDescription.TableStatus;

        public SolidColorBrush TableStatusColor
        {
            get
            {
                Color clr;
                if(string.Equals(this.TableStatus, DynamoDBConstants.TABLE_STATUS_ACTIVE, StringComparison.CurrentCultureIgnoreCase))
                    clr = Colors.Green;
                else
                    clr = Colors.Red;

                return new SolidColorBrush(clr);
            }
        }

        public KeySchemaExtendedElement HashKeyElement { get; private set; }

        public KeySchemaExtendedElement RangeKeyElement { get; private set; }

        public ObservableCollection<KeySchemaExtendedElement> SecondaryIndexes { get; }

        public bool CanScan => string.Equals(this.TableStatus, DynamoDBConstants.TABLE_STATUS_ACTIVE, StringComparison.CurrentCultureIgnoreCase);

        public bool HasBinaryKeys
        {
            get
            {
                bool areBinaryKeys = false;
                if (this._tableDescription != null)
                {
                    areBinaryKeys |= this.HashKeyElement.AttributeType.Equals(DynamoDBConstants.TYPE_BINARY, StringComparison.InvariantCulture);

                    if (this.RangeKeyElement!=null)
                    {
                        areBinaryKeys |= this.RangeKeyElement.AttributeType.Equals(DynamoDBConstants.TYPE_BINARY, StringComparison.InvariantCulture);
                    }
                }
                return areBinaryKeys;
            }
        }

        ObservableCollection<ScanCondition> _scanConditions = new ObservableCollection<ScanCondition>();
        public ObservableCollection<ScanCondition> ScanConditions => this._scanConditions;


        ObservableCollection<Document> _documents = new ObservableCollection<Document>();
        public ObservableCollection<Document> Documents => this._documents;

        public IList<Document> _deletedDocuments = new List<Document>();
        public IList<Document> DeletedDocuments => this._deletedDocuments;

        void onDocumentsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Document doc in e.OldItems)
                {
                    this._deletedDocuments.Add(doc);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this._deletedDocuments.Clear();
            }
        }


        bool _hasData;
        public bool HasData
        {
            get => this._hasData;
            set
            {
                this._hasData = value;
                base.NotifyPropertyChanged("HasData");
            }
        }

        bool _hasMoreRows;
        public bool HasMoreRows
        {
            get => this._hasMoreRows;
            set
            {
                this._hasMoreRows = value;
                base.NotifyPropertyChanged("HasMoreRows");
            }
        }

        string _settingsKey;
        public string SettingsKey
        {
            get => this._settingsKey;
            set
            {
                this._settingsKey = value;
                base.NotifyPropertyChanged("SettingsKey");
            }
        }

        private void SetHashAndRangeKeys()
        {
            var hashKey = _tableDescription.KeySchema.Single(
                        k => k.KeyType == KeyType.HASH);
            var hashAttributeType = _tableDescription.AttributeDefinitions.Single(
                        a => a.AttributeName.Equals(hashKey.AttributeName, StringComparison.InvariantCulture)).AttributeType;

            this.HashKeyElement = new KeySchemaExtendedElement
            {
                AttributeName = hashKey.AttributeName,
                AttributeType = hashAttributeType,
                KeyType = hashKey.KeyType,
                IsPrimaryKeyElement = true
            };

            var rangeKey = _tableDescription.KeySchema.SingleOrDefault(
                k => k.KeyType == KeyType.RANGE);

            if (rangeKey != null)
            {
                var rangeKeyAttributeType = _tableDescription.AttributeDefinitions.Single(
                            a => a.AttributeName.Equals(rangeKey.AttributeName, StringComparison.InvariantCulture)).AttributeType;

                this.RangeKeyElement = new KeySchemaExtendedElement
                {
                    AttributeName = rangeKey.AttributeName,
                    AttributeType = rangeKeyAttributeType,
                    KeyType = rangeKey.KeyType,
                    IsPrimaryKeyElement = true
                };

                this.SecondaryIndexes.Clear();

                if (_tableDescription.LocalSecondaryIndexes != null)
                {
                    // Secondary Indexes
                    foreach (var index in _tableDescription.LocalSecondaryIndexes)
                    {
                        var secondaryRangeKey = index.KeySchema.Single(k => k.KeyType.Equals(KeyType.RANGE));
                        var secondaryRangeKeyAttributeType = _tableDescription.AttributeDefinitions.Single(
                                a => a.AttributeName.Equals(secondaryRangeKey.AttributeName, StringComparison.InvariantCulture)).AttributeType;

                        // Add unique attributes to SecondaryIndexes collection. 
                        // There can be muliple secondary indexes on a table that use the same attribute to index.
                        if (this.SecondaryIndexes.FirstOrDefault(
                            s => s.AttributeName.Equals(secondaryRangeKey.AttributeName, StringComparison.InvariantCulture)) == null)
                        {
                            this.SecondaryIndexes.Add(new KeySchemaExtendedElement
                            {
                                AttributeName = secondaryRangeKey.AttributeName,
                                AttributeType = secondaryRangeKeyAttributeType,
                                KeyType = secondaryRangeKey.KeyType,
                                IsPrimaryKeyElement = false
                            });
                        }
                    }
                }
            }
        }
    }

    public class KeySchemaExtendedElement : KeySchemaElement
    {
        public string AttributeType { get; set; }

        public bool IsPrimaryKeyElement { get; set; }
    }
}
