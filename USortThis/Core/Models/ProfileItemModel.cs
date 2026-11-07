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

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileItemModel"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the profile.</param>
        /// <param name="name">The name of the profile.</param>
        public ProfileItemModel(long id, string name) : base(id)
        {
            _name = name;
            _status = ProfileStatus.Idle;
            _folderBridges = [];
        }

        /// <summary>
        /// Gets the bridge from the source and destination paths.
        /// </summary>
        /// <param name="sourceFolderPath">The source folder path.</param>
        /// <param name="destinationFolderPath">The destination folder path.</param>
        /// <returns>The <see cref="FolderBridgeModel"/> associated with the provided source and destination paths, or null if none found.</returns>
        public FolderBridgeModel? GetBridgeFromSourceAndDestinationPaths(string sourceFolderPath, string destinationFolderPath)
        {
            var bridge = _folderBridges.FirstOrDefault(b =>
                string.Equals(b.SourceFolder?.FolderPath, sourceFolderPath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.DestinationFolder?.FolderPath, destinationFolderPath, StringComparison.OrdinalIgnoreCase));

            return bridge;
        }

        /// <summary>
        /// Adds the folder bridge to the profile.
        /// </summary>
        /// <param name="bridge">The folder bridge to add.</param>
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

        /// <summary>
        /// Removes the folder bridge from the profile.
        /// </summary>
        /// <param name="bridge">The folder bridge to remove.</param>
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

        /// <summary>
        /// Gets the list of folder bridges.
        /// </summary>
        /// <returns>A list of <see cref="FolderBridgeModel"/> objects associated with the profile.</returns>
        public List<FolderBridgeModel> GetFolderBridges() => _folderBridges;

        /// <summary>
        /// Replaces the folder bridges in the profile with the provided collection.
        /// </summary>
        /// <param name="bridges">The collection of folder bridges to replace the existing ones.</param>
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
