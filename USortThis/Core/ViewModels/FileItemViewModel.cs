using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;
using USortThis.UI.Views.Controls;

namespace USortThis.Core.ViewModels
{
    public class FileItemViewModel : ViewModelBase
    {
        private readonly FileItemModel _model;

        public FileItemModel File => _model;


        public string StatusText => _localizationService.Get($"FileItem.{FileItemModel.StatusToString(_model.Status)}");
        public string FailureReasonText => _localizationService.Get($"FileItem.{FileItemModel.FailedReasonToString(_model.FailureReason)}");
        public string ProgressLabelText => _localizationService.Get($"FileItem.Progress");
        public double Progress => _model.Progress;
        public DateTime CreationTime => _model.CreationTime;
        public DateTime LastWriteTime => _model.LastWriteTime;
        public long FileSize => _model.FileSize;

        public FileItemViewModel(FileItemModel copyQueueItem, ILocalizationService localizationService) : base(localizationService)
        {
            _model = copyQueueItem;

            _model.PropertyChanged += (s, e) =>
            {
                SubscribePropertyChanged(e, nameof(StatusText), nameof(FileItemModel.Status));
                SubscribePropertyChanged(e, nameof(FailureReasonText), nameof(FileItemModel.FailureReason));
                SubscribePropertyChanged(e, nameof(Progress), nameof(FileItemModel.Progress));
                SubscribePropertyChanged(e, nameof(CreationTime), nameof(FileItemModel.CreationTime));
                SubscribePropertyChanged(e, nameof(LastWriteTime), nameof(FileItemModel.LastWriteTime));
                SubscribePropertyChanged(e, nameof(FileSize), nameof(FileItemModel.FileSize));
            };

            _localizationService.LanguageChanged += () =>
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(FailureReasonText));
                OnPropertyChanged(nameof(ProgressLabelText));
            };
        }
    }
}
