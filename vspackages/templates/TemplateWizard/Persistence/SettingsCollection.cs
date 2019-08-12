using System;
using System.IO;
using System.Collections.Generic;
using TemplateWizard.ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.Persistence
{
    public class SettingsCollection : IEnumerable<SettingsCollection.ObjectSettings>
    {
        Dictionary<string, Dictionary<string, object>> _values;
        public SettingsCollection()
        {
            this._values = new Dictionary<string, Dictionary<string, object>>();
        }

        public SettingsCollection(Dictionary<string, Dictionary<string, object>> values)
        {
            this._values = values;
        }

        public int Count => this._values.Count;

        internal void Persist(StreamWriter writer)
        {
            JsonWriter jsonWriter = new JsonWriter();
            jsonWriter.PrettyPrint = true;

            jsonWriter.WriteObjectStart();
            foreach (var key in this._values.Keys)
            {
                ObjectSettings os = this[key];
                jsonWriter.WritePropertyName(key);
                os.WriteToJson(jsonWriter);
            }
            jsonWriter.WriteObjectEnd();

            string content = jsonWriter.ToString();
            writer.Write(content);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<SettingsCollection.ObjectSettings> GetEnumerator()
        {
            foreach (var key in this._values.Keys)
            {
                ObjectSettings os = this[key];
                yield return os;
            }
        }

        public ObjectSettings this[string key]
        {
            get 
            {
                Dictionary<string, object> values;
                if (!this._values.TryGetValue(key, out values))
                {
                    return NewObjectSettings(key);
                }

                return new ObjectSettings(key, values);
            }
        }

        public ObjectSettings NewObjectSettings()
        {
            string uniqueKey = Guid.NewGuid().ToString();
            return NewObjectSettings(uniqueKey);
        }

        public ObjectSettings NewObjectSettings(string uniqueKey)
        {
            Dictionary<string, object> backStore = new Dictionary<string, object>();
            ObjectSettings settings = new ObjectSettings(uniqueKey, backStore);
            this._values[uniqueKey] = backStore;
            return settings;
        }

        public void Remove(string uniqueKey)
        {
            this._values.Remove(uniqueKey);
        }

        public void Clear()
        {
            this._values.Clear();
        }

        public class ObjectSettings
        {
            string _uniqueKey;
            Dictionary<string, object> _values;

            internal ObjectSettings(string uniqueKey, Dictionary<string, object> values)
            {
                this._uniqueKey = uniqueKey;
                this._values = values;
            }

            public string UniqueKey => this._uniqueKey;

            public string this[string key]
            {
                get 
                {
                    object o;
                    this._values.TryGetValue(key, out o);
                    return o as string; 
                }
                set => this._values[key] = value;
            }

            public void Remove(string key)
            {
                this._values.Remove(key);
            }

            internal void WriteToJson(JsonWriter writer)
            {
                writer.WriteObjectStart();
                foreach (var kvp in this._values)
                {
                    writer.WritePropertyName(kvp.Key);

                    string value = kvp.Value as string;
                    if (PersistenceManager.Instance.IsEncrypted(kvp.Key) || PersistenceManager.Instance.IsEncrypted(this._uniqueKey))
                        value = UserCrypto.Encrypt(value);

                    writer.Write(value);
                }
                writer.WriteObjectEnd();
            }
        }
    }
}
