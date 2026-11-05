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
    public class FolderItemViewModel : ViewModelBase
    {
        private readonly FolderItemModel _model;

        public string DisplayName => _model.CustomName;
        public string FolderPath => _model.FolderPath;
        public string Status => _localizationService.Get($"FolderItem.{FolderItemModel.StatusToString(_model.Status)}");
        public long FileCount => _model.FileCount;

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
