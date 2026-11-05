using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    public class ProfileItemModel : ListModelBase<FolderBridgeModel>
    {
        public enum ConflictHandlingMode
        {
            Skip,
            Overwrite,
            KeepBoth,
            AskEachTime
        }

        public enum ProfileStatus
        {
            Idle,
            WaitingForSource,
            WaitingForDestination,
            WaitingForProfile,
            InProgress
        }

        private readonly List<FolderBridgeModel> _folderBridges;
        private string _name;
        private ProfileStatus _status;
        private ConflictHandlingMode _conflictHandling = ConflictHandlingMode.Overwrite;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }
        public ProfileStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }
        public ConflictHandlingMode ConflictHandling
        {
            get => _conflictHandling;
            set
            {
                if (_conflictHandling == value) return;
                _conflictHandling = value;
                OnPropertyChanged();
            }
        }
        public double Progress => _folderBridges.Count == 0 ? 0 : _folderBridges.Average(b => b.Progress);

        public ProfileItemModel(long id, string name) : base(id)
        {
            _name = name;
            _status = ProfileStatus.Idle;
            _folderBridges = [];
        }

        public FolderBridgeModel? GetBridgeFromSourceAndDestinationPaths(string sourceFolderPath, string destinationFolderPath)
        {
            var bridge = _folderBridges.FirstOrDefault(b =>
                string.Equals(b.SourceFolder?.FolderPath, sourceFolderPath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.DestinationFolder?.FolderPath, destinationFolderPath, StringComparison.OrdinalIgnoreCase));

            return bridge;
        }

        public void AddFolderBridge(FolderBridgeModel bridge)
        {
            try
            {
                _folderBridges.Add(bridge);
                OnItemAdded(bridge);
            }
            catch
            {

            }
        }

        public void RemoveFolderBridge(FolderBridgeModel bridge)
        {
            try
            {
                _folderBridges.Remove(bridge);
                OnItemRemoved(bridge);
            }
            catch
            {

            }
        }

        public List<FolderBridgeModel> GetFolderBridges() => _folderBridges;

        public void ReplaceFolderBridges(IEnumerable<FolderBridgeModel> bridges)
        {
            foreach (var bridge in _folderBridges.ToList())
                OnItemRemoved(bridge);

            _folderBridges.Clear();
            foreach (var bridge in bridges)
            {
                _folderBridges.Add(bridge);
                OnItemAdded(bridge);
            }

            OnPropertyChanged(nameof(Progress));
        }
    }
}
