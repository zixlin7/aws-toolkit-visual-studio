using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CommonUI.ResourceTags
{
    public class ResourceTag
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsReadOnlyKey { get; set; }
    }

    /// <summary>
    /// 'Standard' tags model which has preset and readonly tag key of 'Name'
    /// and a max limit of 10 tags.
    /// </summary>
    public class ResourceTagsModel : INotifyPropertyChanged
    {
        private ObservableCollection<ResourceTag> _tagList;
        public const string NameKey = "Name";
        public const int MaxTags = 10;

        public ResourceTagsModel()
        {
            _tagList = InitializeEmptyTagSet();
        }

        public ObservableCollection<ResourceTag> Tags
        {
            get { return _tagList; }
            set { _tagList = value; }
        }

        public string NameTag
        {
            get { return _tagList[0].Value; }
            set
            {
                _tagList[0].Value = value;
                NotifyPropertyChanged("Tags");
                NotifyPropertyChanged(NameKey);
            }
        }

        public void SetTag(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Empty key");

            if (string.Equals(key, NameKey, StringComparison.OrdinalIgnoreCase))
            {
                _tagList[0].Value = value;
            }
            else
            {
                // find a gap (or dupe and overwrite)
                int gap = -1;
                for (var i = 1; i < _tagList.Count; i++)
                {
                    if (_tagList[i].Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        _tagList[i].Value = value;
                        return;
                    }
                    else if (gap == -1 && _tagList[i].Key == string.Empty)
                    {
                        gap = i;
                    }
                }

                if (gap != -1)
                {
                    _tagList[gap].Key = key;
                    _tagList[gap].Value = value;
                }
                else
                    throw new ArgumentException(string.Format("Maximum limit of {0} tags has been reached", MaxTags));
            }
        }

        public void SetTags(IDictionary<string, string> tags)
        {
            if (tags.Keys.Count > MaxTags)
                throw new ArgumentException(string.Format("Maximum of {0} tag keys is allowed", MaxTags));

            var newTags = InitializeEmptyTagSet();

            // want to keep Name in position 1, the rest we don't care about
            var nextTagIndex = 1;
            foreach (var tagKey in tags.Keys)
            {
                if (tagKey.Equals(NameKey, StringComparison.OrdinalIgnoreCase))
                    newTags[0].Value = tags[tagKey];
                else
                {
                    newTags[nextTagIndex].Key = tagKey;
                    newTags[nextTagIndex].Value = tags[tagKey];
                    nextTagIndex++;
                }
            }

            Tags = newTags;
            NotifyPropertyChanged("Tags");
        }

        public static ObservableCollection<ResourceTag> InitializeEmptyTagSet()
        {
            var tags = new ObservableCollection<ResourceTag>();

            tags.Add(new ResourceTag { Key = NameKey, Value = "", IsReadOnlyKey = true });
            for (var i = 1; i < MaxTags; i++)
            {
                tags.Add(new ResourceTag { Key = "", Value = "", IsReadOnlyKey = false});
            }

            return tags;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
