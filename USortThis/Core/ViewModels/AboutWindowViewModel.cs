using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using USortThis.Core.Commands;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.ViewModels
{
    /// <summary>
    /// ViewModel for the About window, providing application information such as name, version, copyright, and author.
    /// </summary>
    public class AboutWindowViewModel : ViewModelBase
    {
        private readonly AboutWindowModel _model;

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        public string ApplicationName => _model.ApplicationName;

        /// <summary>
        /// Gets the version of the application.
        /// </summary>
        public string Version => _model.Version;

        /// <summary>
        /// Gets the copyright information of the application.
        /// </summary>
        public string Copyright => _model.Copyright;

        /// <summary>
        /// Gets the author of the application.
        /// </summary>
        public string Author => _model.Author;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutWindowViewModel"/> class.
        /// </summary>
        /// <param name="model">The model containing about information.</param>
        /// <param name="localizationService">The localization service for localizing strings.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="model"/> is null.</exception>
        public AboutWindowViewModel(AboutWindowModel model, ILocalizationService localizationService)
            : base(localizationService)
        {
            ArgumentNullException.ThrowIfNull(model);
            _model = model;

            // Keep view model properties in sync with the model
            _model.PropertyChanged += (s, e) =>
            {
                SubscribePropertyChanged(e, nameof(ApplicationName), nameof(AboutWindowModel.ApplicationName));
                SubscribePropertyChanged(e, nameof(Version), nameof(AboutWindowModel.Version));
                SubscribePropertyChanged(e, nameof(Copyright), nameof(AboutWindowModel.Copyright));
                SubscribePropertyChanged(e, nameof(Author), nameof(AboutWindowModel.Author));
            };
        }
    }
}
