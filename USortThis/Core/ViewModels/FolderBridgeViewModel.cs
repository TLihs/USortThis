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
    public class FolderBridgeViewModel : ViewModelBase
    {
        private readonly FolderBridgeModel _model;
        public FolderBridgeModel Model => _model;

        public string CustomName => _model.CustomName;
        public string SourceFolderPath => _model.SourceFolder?.FolderPath ?? string.Empty;
        public string DestinationFolderPath => _model.DestinationFolder?.FolderPath ?? string.Empty;
        public long PendingTransfers => _model.PendingTransfers;
        public long CompletedTransfers => _model.CompletedTransfers;
        public long FailedTransfers => _model.FailedTransfers;
        public long TotalTransfers => _model.TotalTransfers;
        public double Progress => _model.Progress;

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
