using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Amazon.AWSToolkit.Settings
{
    /// <summary>
    /// File repository to retrieve and store specified settings
    /// </summary>
    /// <typeparam name="T">specified toolkit settings found in this file</typeparam>
    public class FileSettingsRepository<T> : ISettingsRepository<T> where T : class
    {
        private readonly string _filePath;

        public FileSettingsRepository() : this(GetAppDataFileLocation())
        {
        }

        public FileSettingsRepository(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<T> GetOrDefaultAsync(T defaultValue = null)
        {
            if (File.Exists(_filePath))
            {
                return await GetSettingsFromFileAsync<T>() ?? defaultValue;
            }
            return defaultValue;
        }

        public void Save(T settings)
        {
            try
            {
                SaveToFile(settings);
            }
            catch (IOException ex)
            {
                throw new SettingsException($"Error saving to file: {_filePath}", ex);
            }
            catch (JsonException ex)
            {
                throw new SettingsException("Unable to create json object", ex);
            }
        }

        private void SaveToFile(T settings)
        {
            using (var file = File.CreateText(_filePath))
            {
                var serializer = new JsonSerializer()
                {
                    Formatting = Formatting.Indented, DefaultValueHandling = DefaultValueHandling.Populate
                };
                serializer.Serialize(file, settings);
            }
        }

        private async Task<T> GetSettingsFromFileAsync<T>()
        {
            var json = await ReadFileAsync(_filePath);
            return DeserializeSettingsFrom<T>(json);
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
            catch (IOException ex)
            {
                throw new SettingsException($"Could not read file: {filePath}", ex);
            }
        }

        private T DeserializeSettingsFrom<T>(string json)
        {
            try
            {
                var serializerSetting = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                };
                return JsonConvert.DeserializeObject<T>(json, serializerSetting);
            }
            catch (JsonException ex)
            {
                throw new SettingsException($"Unable to parse json: {json}", ex);
            }
        }

        private static string GetAppDataFileLocation()
        {
            //filename is the name assigned to the settings represented by T
            return Utility.GetAppDataSettingsFilePath(typeof(T));
        }
    }
}
