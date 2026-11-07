using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Data
{
    public class VirtualIniReader
    {
        private readonly Dictionary<string, VirtualIniSectionReader> _sections = [];

        /// <summary>Initializes an empty in-memory INI reader.</summary>
        public VirtualIniReader()
        {
        }

        /// <summary>Initializes the reader and loads sections from an INI file.</summary>
        /// <param name="filePath">The path of the file to load.</param>
        public VirtualIniReader(string filePath)
        {
            LoadFromFile(filePath);
        }

        /// <summary>Adds a section or merges its key-value pairs into an existing section.</summary>
        /// <param name="sectionName">The section name.</param>
        /// <param name="keyValues">The values to add to the section.</param>
        private void AddOrUpdateSection(string sectionName, Dictionary<string, string> keyValues)
        {
            if (_sections.TryGetValue(sectionName, out VirtualIniSectionReader? value))
                value.AppendSectionData(keyValues);
            else
                _sections[sectionName] = new VirtualIniSectionReader(sectionName, keyValues);
        }

        /// <summary>Loads INI sections and key-value pairs from a file.</summary>
        /// <param name="filePath">The path of the INI file to read.</param>
        public void LoadFromFile(string filePath)
        {
            using var reader = new StreamReader(filePath);
            string? currentSection = null;
            var sectionData = new Dictionary<string, string>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null)
                    continue;
                // Trim only leading whitespace to preserve values with trailing spaces
                line = line.TrimStart();
                if (line.StartsWith(';') || line.StartsWith('#') || string.IsNullOrEmpty(line))
                    continue; // Skip comments and empty lines

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    if (currentSection != null)
                        AddOrUpdateSection(currentSection, sectionData);

                    currentSection = line[1..^1].Trim();
                    sectionData = [];
                }
                else if (currentSection != null)
                {
                    var keyValue = line.Split('=', 2);
                    if (keyValue.Length > 1)
                    {
                        var key = keyValue[0].Trim();
                        var value = keyValue[1];
                        sectionData.Add(key, value);
                    }
                }
            }

            if (currentSection != null)
                AddOrUpdateSection(currentSection, sectionData);
        }

        /// <summary>Looks up a section-qualified key in the loaded INI data.</summary>
        /// <param name="key">A key in <c>section.key</c> form.</param>
        /// <param name="value">Receives the value when the key is found.</param>
        /// <returns>True when the requested value exists.</returns>
        public bool TryGetValue(string key, out string? value)
        {
            value = null;

            if (!key.Contains('.'))
                return false;

            string section = key.Split('.', 2)[0];
            string keyName = key.Split('.', 2)[1];

            if (!_sections.TryGetValue(section, out var sectionReader))
                return false;

            return sectionReader.TryGetValue(key, out value);
        }

        /// <summary>Gets a loaded section by name.</summary>
        /// <param name="sectionName">The name of the section to retrieve.</param>
        /// <returns>The section reader, or null when no such section is loaded.</returns>
        public VirtualIniSectionReader? GetSection(string sectionName)
        {
            return _sections.TryGetValue(sectionName, out var sectionReader) ? sectionReader : null;
        }

        /// <summary>Removes all loaded sections.</summary>
        public void Clear()
        {
            _sections.Clear();
        }
    }
}
