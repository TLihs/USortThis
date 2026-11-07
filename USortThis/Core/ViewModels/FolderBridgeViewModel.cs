using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.ViewModels
{
    /// <summary>
    /// ViewModel class for bridging folder models and the view.
    /// </summary>
    public class FolderBridgeViewModel : ViewModelBase
    {
        private readonly FolderBridgeModel _model;
        /// <summary>
        /// Gets the underlying model for the folder bridge.
        /// </summary>
        public FolderBridgeModel Model => _model;

        /// <summary>
        /// Gets the custom name of the folder bridge.
        /// </summary>
        public string CustomName => _model.CustomName;
        /// <summary>
        /// Gets the source folder path.
        /// </summary>
        public string SourceFolderPath => _model.SourceFolder?.FolderPath ?? string.Empty;
        /// <summary>
        /// Gets the destination folder path.
        /// </summary>
        public string DestinationFolderPath => _model.DestinationFolder?.FolderPath ?? string.Empty;
        /// <summary>
        /// Gets the number of pending transfers.
        /// </summary>
        public long PendingTransfers => _model.PendingTransfers;
        /// <summary>
        /// Gets the number of completed transfers.
        /// </summary>
        public long CompletedTransfers => _model.CompletedTransfers;
        /// <summary>
        /// Gets the number of failed transfers.
        /// </summary>
        public long FailedTransfers => _model.FailedTransfers;
        /// <summary>
        /// Gets the total number of transfers.
        /// </summary>
        public long TotalTransfers => _model.TotalTransfers;
        /// <summary>
        /// Gets the progress of the folder bridge.
        /// </summary>
        public double Progress => _model.Progress;

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderBridgeViewModel"/> class.
        /// </summary>
        /// <param name="model">The folder bridge model.</param>
        /// <param name="localizationService">The localization service.</param>
        public FolderBridgeViewModel(FolderBridgeModel model, ILocalizationService localizationService) : base(localizationService)
        {
            _model = model;

            _model.PropertyChanged += (s, e) =>
            {
                SubscribePropertyChanged(e, nameof(CustomName), nameof(FolderBridgeModel.CustomName));
                SubscribePropertyChanged(e, nameof(SourceFolderPath), nameof(FolderBridgeModel.SourceFolderPath));
                SubscribePropertyChanged(e, nameof(DestinationFolderPath), nameof(FolderBridgeModel.DestinationFolderPath));
                SubscribePropertyChanged(e, nameof(PendingTransfers), nameof(FolderBridgeModel.PendingTransfers));
                SubscribePropertyChanged(e, nameof(CompletedTransfers), nameof(FolderBridgeModel.CompletedTransfers));
                SubscribePropertyChanged(e, nameof(FailedTransfers), nameof(FolderBridgeModel.FailedTransfers));
                SubscribePropertyChanged(e, nameof(TotalTransfers), nameof(FolderBridgeModel.TotalTransfers));
                SubscribePropertyChanged(e, nameof(Progress), nameof(FolderBridgeModel.Progress));
            };
        }
    }
}
