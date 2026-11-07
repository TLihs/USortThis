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
    /// <summary>
    /// Provides the base class for view models, implementing property change notification
    /// and providing localization support.
    /// </summary>
    /// <param name="localizationService">The localization service used to retrieve localized strings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="localizationService"/> is null.</exception>
    public abstract class ViewModelBase(ILocalizationService localizationService) : NotifyBase
    {
        protected readonly ILocalizationService _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

        /// <summary>
        /// Subscribes to the property changed event for the specified property of the model,
        /// triggering the <see cref="OnPropertyChanged"/> method for the corresponding
        /// view model property.
        /// </summary>
        /// <param name="e">The event data containing information about the property change
        /// event.</param>
        /// <param name="propertyName">The name of the view model property that should be
        /// updated.</param>
        /// <param name="modelPropertyName">The name of the model property whose changes
        /// should be monitored.</param>
        protected void SubscribePropertyChanged(PropertyChangedEventArgs e, string propertyName, string modelPropertyName)
        {
            if (e.PropertyName == modelPropertyName)
                OnPropertyChanged(propertyName);
        }
    }
}
