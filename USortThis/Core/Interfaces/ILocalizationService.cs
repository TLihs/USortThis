using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace USortThis.Core.Interfaces
{
    public interface ILocalizationService
    {
        string Get(string key);
        string CurrentLanguage { get; }
        event Action LanguageChanged;
        void SetLanguage(string languageCode);
    }
}
