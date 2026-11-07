using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Timers;
using System.Windows;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.ViewModels
{
    /// <summary>
    /// ViewModel for representing a folder item in the UI.
    /// </summary>
    public class FolderItemViewModel : ViewModelBase
    {
        private readonly FolderItemModel _model;

        /// <summary>
        /// Gets the display name of the folder item.
        /// </summary>
        public string DisplayName => _model.CustomName;

        /// <summary>
        /// Gets the folder path of the folder item.
        /// </summary>
        public string FolderPath => _model.FolderPath;

        /// <summary>
        /// Gets the status of the folder item.
        /// </summary>
        public string Status => _localizationService.Get($"FolderItem.{FolderItemModel.StatusToString(_model.Status)}");

        /// <summary>
        /// Gets the file count of the folder item.
        /// </summary>
        public long FileCount => _model.FileCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderItemViewModel"/> class.
        /// </summary>
        /// <param name="model">The folder item model.</param>
        /// <param name="localizationService">The localization service.</param>
        public FolderItemViewModel(FolderItemModel model, ILocalizationService localizationService) : base(localizationService)
        {
            _model = model;

            _model.PropertyChanged += (s, e) =>
            {
                SubscribePropertyChanged(e, nameof(DisplayName), nameof(FolderItemModel.CustomName));
                SubscribePropertyChanged(e, nameof(FolderPath), nameof(FolderItemModel.FolderPath));
                SubscribePropertyChanged(e, nameof(Status), nameof(FolderItemModel.Status));
                SubscribePropertyChanged(e, nameof(FileCount), nameof(FolderItemModel.FileCount));
            };
        }
    }
}
