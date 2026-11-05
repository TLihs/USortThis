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
    public class AboutWindowViewModel : ViewModelBase
    {
        private readonly AboutWindowModel _model;

        public string ApplicationName => _model.ApplicationName;
        public string Version => _model.Version;
        public string Copyright => _model.Copyright;
        public string Author => _model.Author;

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
