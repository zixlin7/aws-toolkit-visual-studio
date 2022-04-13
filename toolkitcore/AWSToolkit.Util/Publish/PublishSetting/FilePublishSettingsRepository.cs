using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Settings;

using Newtonsoft.Json;

namespace Amazon.AWSToolkit.Publish.PublishSetting
{
    /// <summary>
    /// Repository to retrieve and store publish related settings
    /// </summary>
    public class FilePublishSettingsRepository : IPublishSettingsRepository
    {
        private readonly string _filePath;

        public FilePublishSettingsRepository() : this(GetAppDataFileLocation())
        {
        }
        public FilePublishSettingsRepository(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<PublishSettings> GetAsync()
        {
            if (File.Exists(_filePath))
            {
                return await GetSettingsFromFileAsync() ?? PublishSettings.CreateDefault();
            }

            return PublishSettings.CreateDefault();
        }

        public void Save(PublishSettings publishSettings)
        {
            try
            {
                SaveToFile(publishSettings);
            }
            catch (IOException e)
            {
                throw new SettingsException($"Error saving to file: {_filePath}", e);
            }
            catch (JsonException ex)
            {
                throw new SettingsException("Unable to create json object", ex);
            }
        }

        private void SaveToFile(PublishSettings publishSettings)
        {
            using (var file = File.CreateText(_filePath))
            {
                // IgnoreAndPopulate is intended for use with debuggable deploy cli properties (which are meant to be overriden by developers manually)
                // It ignores properties which are same as their default value when serializing
                // and set members to their default value when deserializing
                var serializer = new JsonSerializer()
                {
                    Formatting = Formatting.Indented,
                    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                };
                serializer.Serialize(file, publishSettings);
            }
        }

        private async Task<PublishSettings> GetSettingsFromFileAsync()
        {
            var json = await ReadFileAsync(_filePath);
            return DeserializePublishSettingsFrom(json);
        }

        private async Task<string> ReadFileAsync(string filePath)
        {
            try
            {
                using (var reader = File.OpenText(filePath))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (IOException e)
            {
                throw new SettingsException($"Could not read file: {filePath}", e);
            }
        }

        private PublishSettings DeserializePublishSettingsFrom(string json)
        {
            try
            {
                var serializerSetting = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                };
                return JsonConvert.DeserializeObject<PublishSettings>(json, serializerSetting);
            }
            catch (JsonException e)
            {
                throw new SettingsException($"Unable to parse json: {json}", e);
            }
        }

        private static string GetAppDataFileLocation()
        {
            return ToolkitAppDataPath.Join("PublishSettings.json");
        }
    }
}
