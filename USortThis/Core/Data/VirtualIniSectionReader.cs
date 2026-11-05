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

        public VirtualIniSectionReader(string sectionName, Dictionary<string, string> keyValues)
        {
            SectionName = sectionName;
            _keyValues = keyValues;
        }

        public bool TryGetValue(string key, out string? value)
        {
            return _keyValues.TryGetValue(key, out value);
        }

        public void AppendSectionData(Dictionary<string, string> additionalKeyValues)
        {
            foreach (var kvp in additionalKeyValues)
            {
                _keyValues[kvp.Key] = kvp.Value;
            }
        }
    }
}
