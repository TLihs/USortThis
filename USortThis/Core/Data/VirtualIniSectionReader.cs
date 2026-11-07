using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Data
{
    public class VirtualIniSectionReader
    {
        private readonly Dictionary<string, string> _keyValues;

        public string SectionName { get; }

        /// <summary>Initializes a reader for a section and its key-value data.</summary>
        /// <param name="sectionName">The section name.</param>
        /// <param name="keyValues">The values belonging to the section.</param>
        public VirtualIniSectionReader(string sectionName, Dictionary<string, string> keyValues)
        {
            SectionName = sectionName;
            _keyValues = keyValues;
        }

        /// <summary>Looks up a key in this section.</summary>
        /// <param name="key">The key to find.</param>
        /// <param name="value">Receives the value when the key exists.</param>
        /// <returns>True when the key is present.</returns>
        public bool TryGetValue(string key, out string? value)
        {
            return _keyValues.TryGetValue(key, out value);
        }

        /// <summary>Adds or replaces this section's values with the supplied entries.</summary>
        /// <param name="additionalKeyValues">The entries to append or replace.</param>
        public void AppendSectionData(Dictionary<string, string> additionalKeyValues)
        {
            foreach (var kvp in additionalKeyValues)
            {
                _keyValues[kvp.Key] = kvp.Value;
            }
        }
    }
}
