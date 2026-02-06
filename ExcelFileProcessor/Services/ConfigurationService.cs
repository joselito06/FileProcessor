using ExcelFileProcessor.Core.Interfaces;
using ExcelFileProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileProcessor.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly Dictionary<string, object> _settings = new();
        private readonly string _configPath;

        public ConfigurationService(string configPath = null)
        {
            _configPath = configPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            LoadConfiguration();
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is JsonElement jsonElement)
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                    }
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            _settings[key] = value;
            SaveConfiguration();
        }

        public FileSearchConfig LoadFileSearchConfig()
        {
            return new FileSearchConfig
            {
                SearchPaths = GetValue<List<string>>("SearchPaths", new List<string>()),
                FileNames = GetValue<List<string>>("FileNames", new List<string>()),
                FilePatterns = GetValue<List<string>>("FilePatterns", new List<string>()),
                ScheduledTime = TimeSpan.Parse(GetValue("ScheduledTime", "08:00:00")),
                RetryInterval = TimeSpan.Parse(GetValue("RetryInterval", "00:30:00")),
                SearchUntilFound = GetValue("SearchUntilFound", true),
                StopSearchingAfter = GetValue<TimeSpan?>("StopSearchingAfter", null),
                IncludeSubdirectories = GetValue("IncludeSubdirectories", false),
                ExcludePatterns = GetValue<List<string>>("ExcludePatterns", new List<string> { "~$*", "temp_*" }),
                MaxFileSizeBytes = GetValue<long?>("MaxFileSizeBytes", null),
                FileAge = GetValue<TimeSpan?>("FileAge", null)
            };
        }

        public void SaveFileSearchConfig(FileSearchConfig config)
        {
            SetValue("SearchPaths", config.SearchPaths);
            SetValue("FileNames", config.FileNames);
            SetValue("FilePatterns", config.FilePatterns);
            SetValue("ScheduledTime", config.ScheduledTime.ToString());
            SetValue("RetryInterval", config.RetryInterval.ToString());
            SetValue("SearchUntilFound", config.SearchUntilFound);
            SetValue("StopSearchingAfter", config.StopSearchingAfter);
            SetValue("IncludeSubdirectories", config.IncludeSubdirectories);
            SetValue("ExcludePatterns", config.ExcludePatterns);
            SetValue("MaxFileSizeBytes", config.MaxFileSizeBytes);
            SetValue("FileAge", config.FileAge);
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
#if NET472 || NET48
                    var json = File.ReadAllText(_configPath);
#else
                    var json = File.ReadAllText(_configPath);
#endif
                    var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (settings != null)
                    {
                        foreach (var setting in settings)
                        {
                            _settings[setting.Key] = setting.Value;
                        }
                    }
                }
            }
            catch
            {
                // Ignorar errores de carga de configuración
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
#if NET472 || NET48
                File.WriteAllText(_configPath, json);
#else
                File.WriteAllText(_configPath, json);
#endif
            }
            catch
            {
                // Ignorar errores de guardado de configuración
            }
        }
    }
}
