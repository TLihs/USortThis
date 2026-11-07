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
        /// <summary>Gets the localized text for a resource key.</summary>
        /// <param name="key">The key of the localized string.</param>
        string Get(string key);
        string CurrentLanguage { get; }
        event Action LanguageChanged;
        /// <summary>Changes the active language and notifies listeners.</summary>
        /// <param name="languageCode">The language code to activate.</param>
        void SetLanguage(string languageCode);
    }
}
