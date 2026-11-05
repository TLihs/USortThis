using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Data;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Services
{
    public class IniLocalizationService : ILocalizationService
    {
        private readonly string _defaultLanguageCode = "en-US";
        private readonly string _baseLanguagePath = Path.GetFullPath(Path.Combine("Core", "Languages"));
        private readonly VirtualIniReader _baseReader;
        private readonly VirtualIniReader _overrideReader;

        public string CurrentLanguage { get; private set; } = "en-US";

        public event Action? LanguageChanged;

        public IniLocalizationService()
        {
            _baseReader = new VirtualIniReader(Path.GetFullPath(GetBaseLanguageFilePath()));
            _overrideReader = new VirtualIniReader();
        }

        private string GetBaseLanguageFilePath()
        {
            return Path.Combine(_baseLanguagePath, $"{_defaultLanguageCode}.ini");
        }

        public List<string> GetAvailableLanguageFiles()
        {
            string fullLanguagePath = Path.GetFullPath(_baseLanguagePath);

            if (!Directory.Exists(fullLanguagePath))
                return [];

            return [.. Directory.GetFiles(fullLanguagePath, "*.ini")];
        }

        public void SetLanguage(string languageCode)
        {
            CurrentLanguage = languageCode;

            string langFilePath = Path.GetFullPath(Path.Combine(_baseLanguagePath, $"{languageCode}.ini"));
            if (File.Exists(langFilePath))
                _overrideReader.LoadFromFile(langFilePath);
            else
                _overrideReader.Clear();

            LanguageChanged?.Invoke();
        }

        public string Get(string key)
        {
            if (_overrideReader.TryGetValue(key, out var value))
                return value!;
            
            if (_baseReader.TryGetValue(key, out value))
                return value!;

            return $"<{key}>";
        }
    }
}
