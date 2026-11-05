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

        public VirtualIniReader()
        {
        }

        public VirtualIniReader(string filePath)
        {
            LoadFromFile(filePath);
        }

        private void AddOrUpdateSection(string sectionName, Dictionary<string, string> keyValues)
        {
            if (_sections.TryGetValue(sectionName, out VirtualIniSectionReader? value))
                value.AppendSectionData(keyValues);
            else
                _sections[sectionName] = new VirtualIniSectionReader(sectionName, keyValues);
        }

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

        public VirtualIniSectionReader? GetSection(string sectionName)
        {
            return _sections.TryGetValue(sectionName, out var sectionReader) ? sectionReader : null;
        }

        public void Clear()
        {
            _sections.Clear();
        }
    }
}
