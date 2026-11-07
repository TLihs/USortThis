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
    /// <summary>
    /// ViewModel for a file item in the copy queue.
    /// </summary>
    public class FileItemViewModel : ViewModelBase
    {
        private readonly FileItemModel _model;

        /// <summary>
        /// Gets the underlying <see cref="FileItemModel"/> for this view model.
        /// </summary>
        public FileItemModel File => _model;


        /// <summary>
        /// Gets the localized status text for the file item.
        /// </summary>
        public string StatusText => _localizationService.Get($"FileItem.{FileItemModel.StatusToString(_model.Status)}");
        /// <summary>
        /// Gets the localized failure reason text for the file item.
        /// </summary>
        public string FailureReasonText => _localizationService.Get($"FileItem.{FileItemModel.FailedReasonToString(_model.FailureReason)}");
        /// <summary>
        /// Gets the localized progress label text for the file item.
        /// </summary>
        public string ProgressLabelText => _localizationService.Get($"FileItem.Progress");
        /// <summary>
        /// Gets the progress value of the file item.
        /// </summary>
        public double Progress => _model.Progress;
        /// <summary>
        /// Gets the creation time of the file item.
        /// </summary>
        public DateTime CreationTime => _model.CreationTime;
        /// <summary>
        /// Gets the last write time of the file item.
        /// </summary>
        public DateTime LastWriteTime => _model.LastWriteTime;
        /// <summary>
        /// Gets the file size of the file item.
        /// </summary>
        public long FileSize => _model.FileSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileItemViewModel"/> class.
        /// </summary>
        /// <param name="copyQueueItem">The file item model to be wrapped by this view model.</param>
        /// <param name="localizationService">The localization service to be used for localizing strings.</param>
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
