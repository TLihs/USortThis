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
    /// <summary>
    /// Provides localization services using INI files for language-specific settings and text.
    /// </summary>
    public class IniLocalizationService : ILocalizationService
    {
        private readonly string _defaultLanguageCode = "en-US";
        private readonly string _baseLanguagePath = Path.GetFullPath(Path.Combine("Core", "Languages"));
        private readonly VirtualIniReader _baseReader;
        private readonly VirtualIniReader _overrideReader;

        public string CurrentLanguage { get; private set; } = "en-US";

        public event Action? LanguageChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="IniLocalizationService"/> class.
        /// </summary>
        public IniLocalizationService()
        {
            _baseReader = new VirtualIniReader(Path.GetFullPath(GetBaseLanguageFilePath()));
            _overrideReader = new VirtualIniReader();
        }

        private string GetBaseLanguageFilePath()
        {
            return Path.Combine(_baseLanguagePath, $"{_defaultLanguageCode}.ini");
        }

        /// <summary>
        /// Retrieves a list of available language files (INI files) in the language directory.
        /// </summary>
        /// <returns>
        /// A list of file paths for the available language files.
        /// If the directory does not exist, an empty list is returned.
        /// </returns>
        public List<string> GetAvailableLanguageFiles()
        {
            string fullLanguagePath = Path.GetFullPath(_baseLanguagePath);

            if (!Directory.Exists(fullLanguagePath))
                return [];

            return [.. Directory.GetFiles(fullLanguagePath, "*.ini")];
        }

        /// <summary>
        /// Sets the current language using the specified language code.
        /// Loads the corresponding language file from the language directory.
        /// </summary>
        /// <param name="languageCode">The language code to set as the current language.</param>
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

        /// <summary>
        /// Retrieves a localized string by its key.
        /// First checks the overridden language file, then the base language file.
        /// </summary>
        /// <param name="key">The key of the localized string.</param>
        /// <returns>
        /// The localized string if found; otherwise, the key enclosed in angle brackets (e.g., &lt;key&gt;).
        /// </returns>
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
