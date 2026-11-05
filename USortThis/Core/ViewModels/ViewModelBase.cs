using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Data;
using USortThis.Core.Interfaces;

namespace USortThis.Core.ViewModels
{
    public abstract class ViewModelBase(ILocalizationService localizationService) : NotifyBase
    {
        protected readonly ILocalizationService _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        protected void SubscribePropertyChanged(PropertyChangedEventArgs e, string propertyName, string modelPropertyName)
        {
            if (e.PropertyName == modelPropertyName)
                OnPropertyChanged(propertyName);
        }
    }
}
